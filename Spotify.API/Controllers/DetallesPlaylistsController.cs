using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spotify.Modelos;

namespace Spotify.API.Controllers
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
            var appDbContext = _context.DetallesPlaylist.Include(d => d.Cancion).Include(d => d.Playlist);
            return View(await appDbContext.ToListAsync());
        }

        // GET: DetallesPlaylists/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallesPlaylist = await _context.DetallesPlaylist
                .Include(d => d.Cancion)
                .Include(d => d.Playlist)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (detallesPlaylist == null)
            {
                return NotFound();
            }

            return View(detallesPlaylist);
        }

        // GET: DetallesPlaylists/Create
        public IActionResult Create()
        {
            ViewData["CancionId"] = new SelectList(_context.Canciones, "Id", "Genero");
            ViewData["PlaylistId"] = new SelectList(_context.Playlists, "Id", "Nombre");
            return View();
        }

        // POST: DetallesPlaylists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PlaylistId,CancionId")] DetallesPlaylist detallesPlaylist)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detallesPlaylist);  // Usa _context.DetallesPlaylist
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CancionId"] = new SelectList(_context.Canciones, "Id", "Genero", detallesPlaylist.CancionId);
            ViewData["PlaylistId"] = new SelectList(_context.Playlists, "Id", "Nombre", detallesPlaylist.PlaylistId);
            return View(detallesPlaylist);
        }

        // GET: DetallesPlaylists/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallesPlaylist = await _context.DetallesPlaylist.FindAsync(id);
            if (detallesPlaylist == null)
            {
                return NotFound();
            }
            ViewData["CancionId"] = new SelectList(_context.Canciones, "Id", "Genero", detallesPlaylist.CancionId);
            ViewData["PlaylistId"] = new SelectList(_context.Playlists, "Id", "Nombre", detallesPlaylist.PlaylistId);
            return View(detallesPlaylist);
        }

        // POST: DetallesPlaylists/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PlaylistId,CancionId")] DetallesPlaylist detallesPlaylist)
        {
            if (id != detallesPlaylist.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detallesPlaylist);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DetallesPlaylistExists(detallesPlaylist.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CancionId"] = new SelectList(_context.Canciones, "Id", "Genero", detallesPlaylist.CancionId);
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
                .Include(d => d.Cancion)
                .Include(d => d.Playlist)
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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DetallesPlaylistExists(int id)
        {
            return _context.DetallesPlaylist.Any(e => e.Id == id);
        }
    }
}
