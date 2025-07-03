using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using Spotify.MVC.ViewModels;

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

            // Verificar si el email ya existe
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

        // POST: /Cuenta/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string contraseña, bool recordarme = false)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(contraseña))
            {
                ModelState.AddModelError("", "Email y contraseña son requeridos");
                return View();
            }

            var usuario = await _usuarioService.ValidarUsuarioAsync(email, contraseña);
            if (usuario != null)
            {
                // Aquí puedes implementar autenticación con cookies/JWT
                HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
                HttpContext.Session.SetString("TipoUsuario", usuario.TipoUsuario);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email o contraseña incorrectos");
            return View();
        }

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
