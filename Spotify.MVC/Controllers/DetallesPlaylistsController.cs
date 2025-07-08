using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spotify.Modelos;

namespace Spotify.MVC.Controllers
{
    public class DetallesPlaylistsController : Controller
    {
        private readonly AppDbContext _context;

        public DetallesPlaylistsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DetallesPlaylists
        public async Task<IActionResult> Index()
        {
            var detallesPlaylist = await _context.DetallesPlaylist
                .Include(d => d.Playlist)  // Incluye la información de la playlist
                .Include(d => d.Cancion)   // Incluye la información de la canción
                .ToListAsync();
            return View(detallesPlaylist);
        }

        // GET: DetallesPlaylists/Create
        public IActionResult Create()
        {
            // Puedes agregar las canciones y playlists para seleccionarlas en el formulario
            ViewData["CancionId"] = new SelectList(_context.Canciones, "Id", "Titulo");
            ViewData["PlaylistId"] = new SelectList(_context.Playlists, "Id", "Nombre");
            return View();
        }

        // POST: DetallesPlaylists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PlaylistId,CancionId")] DetallesPlaylist detallesPlaylist)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detallesPlaylist);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CancionId"] = new SelectList(_context.Canciones, "Id", "Titulo", detallesPlaylist.CancionId);
            ViewData["PlaylistId"] = new SelectList(_context.Playlists, "Id", "Nombre", detallesPlaylist.PlaylistId);
            return View(detallesPlaylist);
        }

        // GET: DetallesPlaylists/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallesPlaylist = await _context.DetallesPlaylist
                .Include(d => d.Playlist)
                .Include(d => d.Cancion)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (detallesPlaylist == null)
            {
                return NotFound();
            }

            return View(detallesPlaylist);
        }

        // POST: DetallesPlaylists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detallesPlaylist = await _context.DetallesPlaylist.FindAsync(id);
            if (detallesPlaylist != null)
            {
                _context.DetallesPlaylist.Remove(detallesPlaylist);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
