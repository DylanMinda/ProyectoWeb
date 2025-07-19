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
            _autoService = autoService;
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
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(contraseña, usuario.Contraseña))
            {
                ViewBag.ErrorMessage = "Email o contraseña no son correctos";
                return View("Index");
            }

            // Crear los claims para el usuario autenticado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim("TipoUsuario", usuario.TipoUsuario)
            };

            // Solo agrega el claim ArtistaId si el usuario es artista
            if (usuario.TipoUsuario == "artista")
            {
                claims.Add(new Claim("ArtistaId", usuario.Id.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Iniciar sesión con las cookies
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            Console.WriteLine("Inicio de sesión exitoso.");

            var cookie = Request.Cookies[CookieAuthenticationDefaults.AuthenticationScheme];
            if (cookie != null)
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


        //    [HttpPost]
        //    public async Task<IActionResult> Login(string email, string contraseña)
        //    { // Verificar las credenciales del usuario
        //        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        //        if (!BCrypt.Net.BCrypt.Verify(contraseña, usuario.Contraseña))
        //        {
        //            ViewBag.ErrorMessage = "Email o contraseña no son correctos";
        //            return View("Index");
        //        }

        //        var claims = new List<Claim>
        //{
        //            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        //            new Claim(ClaimTypes.Name, usuario.Nombre),
        //            new Claim(ClaimTypes.Email, usuario.Email),
        //            new Claim("TipoUsuario", usuario.TipoUsuario)
        //        };      

        //        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        //        var principal = new ClaimsPrincipal(identity);

        //        // Guardar el ID del usuario en la sesión
        //        HttpContext.Session.SetInt32("UserId", usuario.Id);

        //        // Verificar el tipo de usuario y redirigir al dashboard correspondiente
        //        if (usuario.TipoUsuario == "artista")
        //        {
        //            return RedirectToAction("DashboardArtista", "Home");  // Redirige al dashboard de artista
        //        }
        //        else if (usuario.TipoUsuario == "admin")
        //        {
        //            return RedirectToAction("DashboardAdmin", "Home");  // Redirige al dashboard del admin
        //        }
        //        else if (usuario.TipoUsuario == "cliente")
        //        {
        //            return RedirectToAction("Dashboard", "Home");  // Redirige al home del cliente
        //        }

        //        // Redirige al home en caso de que no sea ninguno de los anteriores (esto debería ser un caso no esperado)
        //        return RedirectToAction("Index", "Home");
        //    }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string nombre, string contraseña, string confirmPassword)
        {
            // Validar nombre
            var nameValidation = PasswordValidator.ValidateName(nombre);
            if (!nameValidation.IsValid)
            {
                ViewBag.ErrorMessage = nameValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View();
            }

            // Validar email
            var emailValidation = PasswordValidator.ValidateEmail(email);
            if (!emailValidation.IsValid)
            {
                ViewBag.ErrorMessage = emailValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View();
            }

            // Validar contraseña
            var passwordValidation = PasswordValidator.ValidatePassword(contraseña);
            if (!passwordValidation.IsValid)
            {
                ViewBag.ErrorMessage = passwordValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View();
            }

            // Validar confirmación de contraseña
            var confirmValidation = PasswordValidator.ValidatePasswordConfirmation(contraseña, confirmPassword);
            if (!confirmValidation.IsValid)
            {
                ViewBag.ErrorMessage = confirmValidation.FirstError;
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View();
            }

            // Verificar si el usuario ya existe
            var usuarioExistente = await _context.Usuarios
                                                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (usuarioExistente != null)
            {
                ViewBag.ErrorMessage = "El correo electrónico ya está registrado.";
                ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
                return View();
            }

            // Si todo está correcto, crear el nuevo usuario
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(contraseña);
            var nuevoUsuario = new Usuario
            {
                Email = email.ToLower().Trim(),
                Nombre = nombre.Trim(),
                Contraseña = hashedPassword,
                TipoUsuario = "cliente",
                FechaRegistro = DateTime.UtcNow
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            ViewBag.SuccessMessage = "Registro exitoso. Ahora puedes iniciar sesión.";
            return RedirectToAction("Index", "Login");
        }
        [HttpGet]
        public IActionResult RegisterArtista()
        {
            ViewBag.PasswordRequirements = PasswordValidator.GetPasswordRequirements();
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
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario == null)
            {
                ViewBag.ErrorMessage = "El correo electrónico no está registrado.";
                return View("Index");
            }
            // Enviar correo electrónico de recuperación de contraseña
            await _emailService.enviarEmailRecuperacionContraseña(email);
            ViewBag.SuccessMessage = "Se ha enviado un correo electrónico para recuperar la contraseña.";
            return RedirectToAction("Index", "Login");
        }

        public IActionResult Logout()
        {
            // Aquí puedes eliminar la sesión o las cookies si las usas
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}
    