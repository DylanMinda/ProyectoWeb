using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using Spotify.MVC.Services;
using System.Security.Claims;

namespace Spotify.MVC.Controllers
{
    public class LoginController : Controller
    {
        private readonly IAutorizarService _autoService;
        private readonly AppDbContext _context;  // Cambiado a AppDbContext
        private readonly IEmailService _emailService;
        private readonly IUsuarioService _usuarioService;

        // Modificado el constructor para inyectar el contexto de la base de datos
        public LoginController(IAutorizarService autoService, AppDbContext context, IEmailService emailService, IUsuarioService usuarioService)
        {
            _autoService = autoService;// Asignamos el servicio de autorización aquí
            _context = context;  // Asignamos el contexto aquí
            _emailService = emailService;
            _usuarioService = usuarioService;
        }


        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string email, string contraseña)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email); // Buscar el usuario por email 

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(contraseña, usuario.Contraseña)) // Verificar si el usuario existe y si la contraseña es correcta
            {
                ViewBag.ErrorMessage = "Email o contraseña no son correctos";
                return View("Index");
            }

            // Crear los claims para el usuario autenticado
            var claims = new List<Claim> // Lista de claims que se asignarán al usuario
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()), // Identificador único del usuario
                new Claim(ClaimTypes.Name, usuario.Nombre),// Nombre del usuario
                new Claim(ClaimTypes.Email, usuario.Email),// Email del usuario
                new Claim("TipoUsuario", usuario.TipoUsuario)// Tipo de usuario (artista, admin, cliente)
            };

            // Solo agrega el claim ArtistaId si el usuario es artista
            if (usuario.TipoUsuario == "artista")// Verifica si el usuario es artista
            {
                claims.Add(new Claim("ArtistaId", usuario.Id.ToString()));// Agrega el claim ArtistaId con el ID del usuario
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);// Crea una identidad de claims con el esquema de autenticación de cookies
            var principal = new ClaimsPrincipal(identity);// Crea un principal de claims a partir de la identidad

            // Iniciar sesión con las cookies
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);// Inicia sesión utilizando el esquema de autenticación de cookies
            Console.WriteLine("Inicio de sesión exitoso.");

            var cookie = Request.Cookies[CookieAuthenticationDefaults.AuthenticationScheme];// Obtiene la cookie de autenticación del contexto HTTP
            if (cookie != null)//verifica si la cookie no es nula
            {
                Console.WriteLine("Cookie de sesión establecida.");
            }
            else
            {
                Console.WriteLine("No se pudo establecer la cookie de sesión.");
            }

            // Redirigir al dashboard correspondiente según el tipo de usuario
            if (usuario.TipoUsuario == "artista")
            {
                return RedirectToAction("DashboardArtista", "Home");
            }
            else if (usuario.TipoUsuario == "admin")
            {
                return RedirectToAction("DashboardAdmin", "Home");
            }
            else if (usuario.TipoUsuario == "cliente")
            {
                return RedirectToAction("Dashboard", "Home");
            }

            return RedirectToAction("Index", "Home");  // Caso no esperado
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements(); // Proporciona los requisitos de contraseña a la vista
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string nombre, string contraseña, string confirmPassword)
        {
            // Validar nombre
            var nameValidation = PasswordValidator.ValidateName(nombre); // Validar el nombre del usuario
            if (!nameValidation.IsValid) // Verifica si la validación del nombre es exitosa
            {
                ViewBag.ErrorMessage = nameValidation.FirstError;// Muestra el primer error de validación del nombre
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();// Proporciona los requisitos de contraseña a la vista
                return View();
            }

            // Validar email
            var emailValidation = PasswordValidator.ValidateEmail(email);// Validar el formato del email
            if (!emailValidation.IsValid)// Verifica si la validación del email es exitosa
            {
                ViewBag.ErrorMessage = emailValidation.FirstError;// Muestra el primer error de validación del email
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();// Proporciona los requisitos de contraseña a la vista
                return View();
            }

            // Validar contraseña
            var passwordValidation = PasswordValidator.ValidatePassword(contraseña);// Validar la fortaleza de la contraseña
            if (!passwordValidation.IsValid)// Verifica si la validación de la contraseña es exitosa
            {
                ViewBag.ErrorMessage = passwordValidation.FirstError;// Muestra el primer error de validación de la contraseña
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();// Proporciona los requisitos de contraseña a la vista
                return View();
            }

            // Validar confirmación de contraseña
            var confirmValidation = PasswordValidator.ValidatePasswordConfirmation(contraseña, confirmPassword);// Validar que la confirmación de contraseña coincida con la contraseña original
            if (!confirmValidation.IsValid)// Verifica si la validación de la confirmación de contraseña es exitosa
            {
                ViewBag.ErrorMessage = confirmValidation.FirstError;// Muestra el primer error de validación de la confirmación de contraseña
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();// Proporciona los requisitos de contraseña a la vista
                return View();
            }

            // Verificar si el usuario ya existe
            var usuarioExistente = await _context.Usuarios
                                                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());// Busca un usuario existente con el mismo email, ignorando mayúsculas y minúsculas
            if (usuarioExistente != null)// Si ya existe un usuario con ese email
            {
                ViewBag.ErrorMessage = "El correo electrónico ya está registrado.";// Muestra un mensaje de error indicando que el email ya está en uso
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();// Proporciona los requisitos de contraseña a la vista
                return View();
            }

            // Si todo está correcto, crear el nuevo usuario
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(contraseña);// Hashea la contraseña utilizando BCrypt para mayor seguridad
            var nuevoUsuario = new Usuario// Crea una nueva instancia de Usuario con los datos proporcionados
            {
                Email = email.ToLower().Trim(),// Asigna el email en minúsculas y sin espacios
                Nombre = nombre.Trim(),// Asigna el nombre sin espacios adicionales
                Contraseña = hashedPassword,// Asigna la contraseña hasheada
                TipoUsuario = "cliente",// Asigna el tipo de usuario como "cliente"
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);// Añade el nuevo usuario al contexto de la base de datos
            await _context.SaveChangesAsync();// Guarda los cambios en la base de datos

            ViewBag.SuccessMessage = "Registro exitoso. Ahora puedes iniciar sesión.";
            return RedirectToAction("Index", "Login");
        }
        [HttpGet]
        public IActionResult RegisterArtista()
        {
            ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();// Proporciona los requisitos de contraseña a la vista
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterArtista(string email, string nombre, string contraseña, string confirmPassword)
        {
            // Validar nombre
            var nameValidation = PasswordValidator.ValidateName(nombre);
            if (!nameValidation.IsValid)
            {
                ViewBag.ErrorMessage = nameValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View("RegisterArtista");
            }

            // Validar email
            var emailValidation = PasswordValidator.ValidateEmail(email);
            if (!emailValidation.IsValid)
            {
                ViewBag.ErrorMessage = emailValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View("RegisterArtista");
            }

            // Validar contraseña
            var passwordValidation = PasswordValidator.ValidatePassword(contraseña);
            if (!passwordValidation.IsValid)
            {
                ViewBag.ErrorMessage = passwordValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View("RegisterArtista");
            }

            // Validar confirmación de contraseña
            var confirmValidation = PasswordValidator.ValidatePasswordConfirmation(contraseña, confirmPassword);
            if (!confirmValidation.IsValid)
            {
                ViewBag.ErrorMessage = confirmValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View("RegisterArtista");
            }

            // Verificar si el usuario ya existe
            var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (usuarioExistente != null)
            {
                ViewBag.ErrorMessage = "Este correo electrónico ya está registrado.";
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View("RegisterArtista");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(contraseña);
            var nuevoUsuario = new Usuario
            {
                Email = email.ToLower().Trim(),
                Nombre = nombre.Trim(),
                Contraseña = hashedPassword,
                TipoUsuario = "artista",
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            ViewBag.SuccessMessage = "Registro exitoso. Ahora puedes iniciar sesión.";
            return RedirectToAction("Index", "Login");
        }
        [HttpGet]
        public IActionResult RecuperarContraseña()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RecuperarContraseña(string email)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email); // Buscar el usuario por email
            if (usuario == null)// Si no se encuentra el usuario con ese email
            {
                ViewBag.ErrorMessage = "El correo electrónico no está registrado.";
                return View("Index");
            }
            // Enviar correo electrónico de recuperación de contraseña
            await _emailService.enviarEmailRecuperacionContraseña(email);// Llama al servicio de email para enviar el correo de recuperación
            ViewBag.SuccessMessage = "Se ha enviado un correo electrónico para recuperar la contraseña.";
            return RedirectToAction("Index", "Login");
        }

        public IActionResult Logout()
        {
            // Aquí puedes eliminar la sesión o las cookies si las usas
            HttpContext.Session.Clear();// Limpia la sesión actual
            return RedirectToAction("Index", "Login");
        }
    }
}
    