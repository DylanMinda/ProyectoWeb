using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using Spotify.MVC.ViewModels;
using System.Security.Claims;

namespace Spotify.MVC.Controllers
{
    public class CuentaController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public CuentaController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: /Cuenta/Registro
        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        // POST: /Cuenta/Registro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Verifica si el email ya está registrado
            if (await _usuarioService.ExisteEmailAsync(modelo.Email))
            {
                ModelState.AddModelError("Email", "Este email ya está registrado");
                return View(modelo);
            }

            try
            {
                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    Nombre = modelo.Nombre,
                    Email = modelo.Email,
                    Contraseña = modelo.Contraseña,
                    TipoUsuario = modelo.TipoUsuario,
                    FechaRegistro = DateTime.UtcNow
                };

                var usuarioCreado = await _usuarioService.CrearUsuarioAsync(nuevoUsuario);

                // Redirigir a página de éxito o login
                TempData["MensajeExito"] = "Usuario registrado exitosamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear el usuario: " + ex.Message);
                return View(modelo);
            }
        }

        // GET: /Cuenta/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string contraseña)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(contraseña))
            {
                ModelState.AddModelError("", "Email y contraseña son requeridos");
                return View();
            }

            var usuario = await _usuarioService.ValidarUsuarioAsync(email, contraseña);
            if (usuario != null)
            {
                // Aquí creamos los claims para la autenticación
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim("TipoUsuario", usuario.TipoUsuario)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Iniciar sesión con cookies
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

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

                return RedirectToAction("Index", "Home"); // Caso no esperado
            }

            ModelState.AddModelError("", "Email o contraseña incorrectos");
            return View();
        }
        // POST: /Cuenta/Login
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(string email, string contraseña, bool recordarme = false)
        //{
        //    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(contraseña))
        //    {
        //        ModelState.AddModelError("", "Email y contraseña son requeridos");
        //        return View();
        //    }

        //    var usuario = await _usuarioService.ValidarUsuarioAsync(email, contraseña);
        //    if (usuario != null)
        //    {
        //        //// Aquí puedes implementar autenticación con cookies/JWT
        //        //HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
        //        //HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
        //        //HttpContext.Session.SetString("TipoUsuario", usuario.TipoUsuario);

        //        return RedirectToAction("Index", "Home");
        //    }

        //    ModelState.AddModelError("", "Email o contraseña incorrectos");
        //    return View();
        //}

        // GET: /Cuenta/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Método para verificar email disponible (AJAX)
        [HttpGet]
        public async Task<JsonResult> VerificarEmail(string email)
        {
            bool existe = await _usuarioService.ExisteEmailAsync(email);
            return Json(!existe);
        }
    }
}
