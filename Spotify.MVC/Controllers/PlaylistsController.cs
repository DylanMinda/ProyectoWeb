using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.Modelos;
using System.Security.Claims;

namespace Spotify.MVC.Controllers
{
    [Authorize]
    public class PlaylistsController : Controller
    {
        private readonly AppDbContext _context;

        public PlaylistsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Playlists/Index
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("Usuario no autenticado. Redirigiendo al login...");
                return RedirectToAction("Index", "Login");
            }

            // 1. Obtenemos el Id del usuario logueado
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // 2. Filtramos solo las playlists de este usuario
            var playlists = await _context.Playlists
                .Where(p => p.UsuarioId == userId)
                .Include(p => p.DetallesPlaylists)
                    .ThenInclude(d => d.Cancion)
                .ToListAsync();

            return View(playlists);
        }

        // GET: Playlists/Create
        public async Task<IActionResult> Create(string searchTerm = "", List<int> selectedSongs = null)
        {
            ViewBag.SearchTerm = searchTerm;

            // Obtener todas las canciones y aplicar filtro de búsqueda si es necesario
            var cancionesQuery = _context.Canciones.Include(c => c.Album).ThenInclude(a => a.Artista).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                cancionesQuery = cancionesQuery.Where(c =>
                    c.Titulo.Contains(searchTerm) ||
                    c.Album.Nombre.Contains(searchTerm) ||
                    c.Album.Artista.Nombre.Contains(searchTerm));
            }

            var canciones = await cancionesQuery.Take(20).ToListAsync();
            ViewBag.Canciones = canciones;

            // Mantener las canciones seleccionadas
            if (selectedSongs != null && selectedSongs.Any())
            {
                var cancionesSeleccionadas = canciones.Where(c => selectedSongs.Contains(c.Id)).ToList();
                ViewBag.CancionesSeleccionadas = cancionesSeleccionadas;
                ViewBag.SelectedSongIds = selectedSongs;
            }
            else
            {
                ViewBag.CancionesSeleccionadas = new List<Cancion>();
                ViewBag.SelectedSongIds = new List<int>();
            }

            return View(new Playlist { FechaCreacion = DateTime.Now });
        }
        // POST: Buscar canciones
        [HttpPost]
        public async Task<IActionResult> SearchSongs(string searchTerm, List<int> selectedSongs)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Console.WriteLine("Usuario no autenticado.");
                ViewBag.ErrorMessage = "No estás autenticado.";
                return RedirectToAction("Login", "Account");
            }
            return RedirectToAction("Create", new { searchTerm = searchTerm, selectedSongs = selectedSongs });
        }

        // POST: Agregar canción
        [HttpPost]
        public async Task<IActionResult> AddSong(int songId, string searchTerm, List<int> selectedSongs)
        {
            selectedSongs ??= new List<int>();  // Si es null, inicializa la lista

            if (!selectedSongs.Contains(songId))
            {
                selectedSongs.Add(songId);
            }

            return RedirectToAction("Create", new { searchTerm, selectedSongs });
        }

        // POST: Remover canción
        [HttpPost]
        public async Task<IActionResult> RemoveSong(int songId, string searchTerm, List<int> selectedSongs)
        {
            selectedSongs ??= new List<int>();  // Si es null, inicializa la lista

            selectedSongs.Remove(songId);

            return RedirectToAction("Create", new { searchTerm, selectedSongs });
        }

        // POST: Crear playlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,FechaCreacion")] Playlist playlist, List<int> selectedSongs)
        {
            try
            {
                // Verifica si el modelo es válido
                if (ModelState.IsValid)
                {
                    if (!User.Identity.IsAuthenticated)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    // Obtener el ID del usuario logueado
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    playlist.UsuarioId = userId;
                    playlist.FechaCreacion = DateTime.UtcNow; // Aseguramos que la fecha esté en UTC

                    // Registrar en la consola para verificar la fecha
                    Console.WriteLine($"Creando playlist: {playlist.Nombre} para el usuario con ID {userId} a las {playlist.FechaCreacion}");

                    // Crear la playlist en la base de datos
                    _context.Add(playlist);
                    await _context.SaveChangesAsync();

                    // Verificar si selectedSongs tiene elementos
                    Console.WriteLine($"Canciones seleccionadas: {string.Join(", ", selectedSongs)}");

                    // Agregar las canciones seleccionadas a la playlist (DetallesPlaylist)
                    if (selectedSongs != null && selectedSongs.Any())
                    {
                        foreach (var songId in selectedSongs)
                        {
                            var detallePlaylist = new DetallesPlaylist
                            {
                                PlaylistId = playlist.Id,
                                CancionId = songId
                            };
                            Console.WriteLine($"Agregando canción con ID {songId} a la playlist {playlist.Id}");
                            _context.DetallesPlaylist.Add(detallePlaylist); // Relaciona la canción con la playlist
                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine("No se han seleccionado canciones.");
                    }

                    // Redirigir al Index de Playlists
                    ViewBag.SuccessMessage = "Playlist creada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.ErrorMessage = "Error al crear la playlist. Verifique los campos.";
                return View(playlist);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                Console.WriteLine($"Error: {ex.Message}");
                ViewBag.ErrorMessage = "Ocurrió un error: " + ex.Message;
                return View(playlist);
            }
        }

        //EDIT Y DELETE 

        // GET: Playlists/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var playlist = await _context.Playlists
                .Include(p => p.Creador)
                .Include(p => p.DetallesPlaylists)
                    .ThenInclude(dp => dp.Cancion)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (playlist == null) return NotFound();
            return View(playlist);
        }

        // GET: Playlists/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var playlist = await _context.Playlists
                .Include(p => p.DetallesPlaylists)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (playlist == null) return NotFound();

            // Poblamos ViewBag con todas las canciones y con las ya seleccionadas
            ViewBag.AllSongs = await _context.Canciones.ToListAsync();
            ViewBag.SelectedSongIds = playlist.DetallesPlaylists.Select(d => d.CancionId).ToList();
            return View(playlist);
        }

        // POST: Playlists/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Playlist model, List<int> selectedSongs)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.AllSongs = await _context.Canciones.ToListAsync();
                ViewBag.SelectedSongIds = selectedSongs;
                return View(model);
            }

            // Actualizar datos básicos
            _context.Update(model);
            // Primero eliminamos viejos detalles
            var viejos = _context.DetallesPlaylist.Where(d => d.PlaylistId == id);
            _context.DetallesPlaylist.RemoveRange(viejos);
            // Luego agregamos los nuevos
            foreach (var songId in selectedSongs)
                _context.DetallesPlaylist.Add(new DetallesPlaylist { PlaylistId = id, CancionId = songId });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Playlists/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.Id == id);
            if (playlist == null) return NotFound();
            return View(playlist);
        }

        // POST: Playlists/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
