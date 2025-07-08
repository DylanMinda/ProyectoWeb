using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.Modelos;

namespace Spotify.MVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Vista para el dashboard del Admin
        public IActionResult DashboardAdmin()
        {
            return View();
        }

        // Ver todos los usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.TipoUsuario != "admin") // Excluye los admins si no deseas verlos
                .ToListAsync();

            return View(usuarios);
        }

        // Eliminar usuario (cliente o artista)
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction("Usuarios");
        }

        // Ver todas las canciones
        public async Task<IActionResult> Canciones()
        {
            var canciones = await _context.Canciones
                .Include(c => c.Album)
                .Include(c => c.Album.Artista)
                .ToListAsync();

            return View(canciones);
        }

        // Eliminar canción
        public async Task<IActionResult> EliminarCancion(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
            {
                return NotFound();
            }

            _context.Canciones.Remove(cancion);
            await _context.SaveChangesAsync();

            return RedirectToAction("Canciones");
        }
    }
}
