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
            try
            {
                // 1) Recupera la entidad trackeada con todas las relaciones necesarias
                var existente = await _context.Usuarios
                                      .Include(u => u.Plan)
                                      .Include(u => u.Albums)
                                      .Include(u => u.Canciones)
                                      .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                if (existente == null)
                    return NotFound();

                // 2) Verificar si se está intentando cambiar la contraseña
                bool cambiandoPassword = !string.IsNullOrEmpty(newPassword) || !string.IsNullOrEmpty(confirmPassword);

                // 3) Validaciones condicionales
                if (cambiandoPassword)
                {
                    // Validar contraseña actual solo si se está cambiando
                    if (string.IsNullOrEmpty(currentPassword))
                    {
                        ModelState.AddModelError("currentPassword", "La contraseña actual es requerida para cambiar la contraseña.");
                        return View(existente);
                    }

                    // Verificar contraseña actual
                    if (!BCrypt.Net.BCrypt.Verify(currentPassword, existente.Contraseña))
                    {
                        ModelState.AddModelError("currentPassword", "La contraseña actual es incorrecta.");
                        return View(existente);
                    }

                    // Validar nueva contraseña
                    if (string.IsNullOrEmpty(newPassword))
                    {
                        ModelState.AddModelError("newPassword", "La nueva contraseña es requerida.");
                        return View(existente);
                    }

                    if (newPassword.Length < 6)
                    {
                        ModelState.AddModelError("newPassword", "La contraseña debe tener al menos 6 caracteres.");
                        return View(existente);
                    }

                    // Validar confirmación de contraseña
                    if (newPassword != confirmPassword)
                    {
                        ModelState.AddModelError("confirmPassword", "Las contraseñas no coinciden.");
                        return View(existente);
                    }

                    // Actualizar contraseña
                    existente.Contraseña = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                // 4) Actualizar información básica (siempre se permite)
                existente.Nombre = usuario.Nombre;
                existente.Email = usuario.Email;

                // 5) Guardar cambios
                await _context.SaveChangesAsync();

                // 6) Mensaje de éxito
                TempData["SuccessMessage"] = cambiandoPassword ?
                    "Perfil y contraseña actualizados correctamente." :
                    "Perfil actualizado correctamente.";

                // 7) Solo hacer logout si se cambió la contraseña
                if (cambiandoPassword)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["LoginMessage"] = "Tu contraseña ha sido actualizada. Por favor, inicia sesión nuevamente.";
                    return RedirectToAction("Index", "Login");
                }

                // 8) Si solo se actualizó info básica, redirigir al perfil
                return RedirectToAction("EditarPerfil");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar el perfil: " + ex.Message);

                // En caso de error, recargar la entidad con todas sus relaciones
                var existenteError = await _context.Usuarios
                                           .Include(u => u.Plan)
                                           .Include(u => u.Albums)
                                           .Include(u => u.Canciones)
                                           .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                return View(existenteError ?? usuario);
            }
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
}
