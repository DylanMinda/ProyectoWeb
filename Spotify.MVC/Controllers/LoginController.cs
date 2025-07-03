using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;

namespace Spotify.MVC.Controllers
{
    public class LoginController : Controller
    {
        private readonly IAutorizarService _autoService;
        private readonly IEmailService _emailService;
        public LoginController(IAutorizarService autoService, IEmailService emailService)
        {
            _autoService = autoService;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string contraseña)
        {
            if (await _autoService.Login(email, contraseña))
            {
                await _emailService.enviarEmailBienvenida(email);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.ErrorMessage = "Email o contraseña no son correctas";
                return View("Index");
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string nombre, string contraseña)
        {
            if (await _autoService.Register(email, nombre, contraseña))
            {
                return RedirectToAction("Index", "Login");
            }
            else
            {
                ViewBag.ErrorMessage = "Error al registrar el usuario.";
                return View();
            }
        }
        [HttpGet]
        public IActionResult RecuperarPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RecuperarPassword(string email)
        {
            var usuario = CRUD<Usuario>.GetAll().FirstOrDefault(u => u.Email == email);
            if (usuario == null)
            {
                ViewBag.ErrorMessage = "Este correo no ha sido registrado";
                return View("Index");
            }
            await _emailService.enviarEmailRecuperacionPassword(email);
            ViewBag.SuccessMessage = "Se envio un correo para recuperar su contraseña";
            return RedirectToAction("Index", "Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Login");
        }
    }
}
