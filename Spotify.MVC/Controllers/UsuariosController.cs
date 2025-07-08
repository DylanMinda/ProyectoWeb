using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.APIConsumer;
using Spotify.Modelos;
using System.Security.Claims;

namespace Spotify.MVC.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: UsuariosController
        public ActionResult Index()
        {
            var lista = CRUD<Usuario>.GetAll();
            return View(lista);
        }

        // GET: UsuariosController/Details/5
        public ActionResult Details(int id)
        {
            var usuario = CRUD<Usuario>.GetById(id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        // GET: UsuariosController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UsuariosController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UsuariosController/Edit/5
        public ActionResult Edit(int id)
        {
            var usuario = CRUD<Usuario>.GetById(id);
            if (usuario == null)
            {
                return NotFound();
            }
            ViewBag.TiposUsuario = new List<string> { "cliente", "artista", "admin" };
            return View(usuario);
        }

        // POST: UsuariosController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Usuario usuario)
        {
            try
            {
                // Asegúrate de obtener el usuario por ID y actualizarlo
                var usuarioExistente = CRUD<Usuario>.GetById(id);
                if (usuarioExistente != null)
                {
                    // Actualiza las propiedades necesarias
                    usuarioExistente.Nombre = usuario.Nombre;
                    usuarioExistente.Email = usuario.Email;
                    usuarioExistente.TipoUsuario = usuario.TipoUsuario;
                    // Actualiza el usuario en la base de datos
                    CRUD<Usuario>.Update(id, usuarioExistente);
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditarPerfil()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var usuario = await _context.Usuarios
                                  .Include(u => u.Plan)
                                  .Include(u => u.Albums)
                                  .Include(u => u.Canciones)
                                  .FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // POST: /Usuarios/EditarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(
            Usuario usuario,
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            // 1) Recupera la entidad trackeada
            var existente = await _context.Usuarios
                                  .FirstOrDefaultAsync(u => u.Id == usuario.Id);
            if (existente == null) return NotFound();

            // 2) Validaciones
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, existente.Contraseña))
                    ModelState.AddModelError("currentPassword", "La contraseña actual es incorrecta");
                if (newPassword != confirmPassword)
                    ModelState.AddModelError("confirmPassword", "Las contraseñas no coinciden");
            }

            if (!ModelState.IsValid)
                return View(usuario);

            // 3) DEBUG: muestra el hash **antes** de cambiar
            Console.WriteLine($"[DEBUG] Antes → {existente.Contraseña}");

            // 4) Asigna la nueva contraseña
            if (!string.IsNullOrEmpty(newPassword))
                existente.Contraseña = BCrypt.Net.BCrypt.HashPassword(newPassword);

            existente.Nombre = usuario.Nombre;
            existente.Email = usuario.Email;

            // 5) Persiste cambios
            await _context.SaveChangesAsync();

            // 6) DEBUG: recarga la entidad sin tracking y muestra el hash **después**
            var recargado = await _context.Usuarios
                                  .AsNoTracking()
                                  .FirstAsync(u => u.Id == existente.Id);
            Console.WriteLine($"[DEBUG] Después → {recargado.Contraseña}");

            // 7) Fuerza logout para re-loguear con la nueva clave
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 8) Redirige al login
            return RedirectToAction("Index", "Login");
        }

        // GET: UsuariosController/Delete/5
        public ActionResult Delete(int id)
        {
            var usuario = CRUD<Usuario>.GetById(id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        // POST: UsuariosController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                CRUD<Usuario>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }



    //public class UsuariosController : Controller
    //{
    //    // GET: UsuariosController
    //    public ActionResult Index()
    //    {
    //        var lista = CRUD<Usuario>.GetAll();
    //        return View(lista);
    //    }

    //    // GET: UsuariosController/Details/5
    //    public ActionResult Details(int id)
    //    {
    //        return View();
    //    }

    //    // GET: UsuariosController/Create
    //    public ActionResult Create()
    //    {
    //        return View();
    //    }

    //    // POST: UsuariosController/Create
    //    [HttpPost]
    //    [ValidateAntiForgeryToken]
    //    public ActionResult Create(IFormCollection collection)
    //    {
    //        try
    //        {
    //            return RedirectToAction(nameof(Index));
    //        }
    //        catch
    //        {
    //            return View();
    //        }
    //    }

    //    // GET: UsuariosController/Edit/5
    //    public ActionResult Edit(int id)
    //    {
    //        var usuario = CRUD<Usuario>.GetById(id);
    //        ViewBag.TiposUsuario = new List<string> { "cliente", "artista", "admin" };
    //        return View(usuario);
    //    }

    //    // POST: UsuariosController/Edit/5
    //    [HttpPost]
    //    [ValidateAntiForgeryToken]
    //    public ActionResult Edit(int id, Usuario usuario)
    //    {
    //        try
    //        {
    //            // Asegúrate de obtener el usuario por ID y actualizarlo
    //            var usuarioExistente = CRUD<Usuario>.GetById(id);

    //            if (usuarioExistente != null)
    //            {
    //                // Actualiza las propiedades necesarias
    //                usuarioExistente.Nombre = usuario.Nombre;
    //                usuarioExistente.Email = usuario.Email;
    //                usuarioExistente.TipoUsuario = usuario.TipoUsuario;

    //                // Actualiza el usuario en la base de datos
    //                // Aquí debes hacer la actualización en la base de datos, depende de tu implementación
    //                CRUD<Usuario>.Update(id, usuarioExistente);
    //            }

    //            return RedirectToAction(nameof(Index));
    //        }
    //        catch
    //        {
    //            return View();
    //        }
    //    }

    //    // GET: UsuariosController/Delete/5
    //    public ActionResult Delete(int id)
    //    {
    //        var usuario = CRUD<Usuario>.GetById(id);
    //        return View(usuario);
    //    }

    //    // POST: UsuariosController/Delete/5
    //    [HttpPost]
    //    [ValidateAntiForgeryToken]
    //    public ActionResult Delete(int id, IFormCollection collection)
    //    {
    //        try
    //        {
    //            return RedirectToAction(nameof(Index));
    //        }
    //        catch
    //        {
    //            return View();
    //        }
    //    }
    //}
}
