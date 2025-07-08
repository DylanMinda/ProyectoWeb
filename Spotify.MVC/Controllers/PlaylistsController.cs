using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.Modelos;
using System.Security.Claims;

namespace Spotify.MVC.Controllers
{
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
            var playlists = await _context.Playlists.Include(p => p.DetallesPlaylists).ToListAsync();
            return View(playlists);
        }

        // GET: Playlists/Create
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
                if (ModelState.IsValid)
                {
                    // Obtener el ID del usuario logueado
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);  // Obtener el ID del usuario logueado
                    if (userId != null)
                    {
                        playlist.UsuarioId = int.Parse(userId);  // Convertir el ID de usuario de string a int
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Usuario no autenticado.";
                        return View(playlist);
                    }

                    // Crear la playlist
                    _context.Add(playlist);
                    await _context.SaveChangesAsync();

                    // Agregar las canciones seleccionadas a la playlist
                    if (selectedSongs != null && selectedSongs.Any())
                    {
                        foreach (var songId in selectedSongs)
                        {
                            var detallePlaylist = new DetallesPlaylist
                            {
                                PlaylistId = playlist.Id,
                                CancionId = songId
                            };
                            _context.DetallesPlaylist.Add(detallePlaylist);
        
                        }
                        await _context.SaveChangesAsync();
                    }

                    ViewBag.SuccessMessage = "Playlist creada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.ErrorMessage = "Error al crear la playlist.";
                return View(playlist);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                ViewBag.ErrorMessage = "Ocurrió un error: " + ex.Message;
                return View(playlist);
            }
        }

        // Método auxiliar para agregar canciones seleccionadas a la playlist
        private async Task AddSelectedSongsToPlaylist(int playlistId, List<int> selectedSongs)
        {
            if (selectedSongs != null && selectedSongs.Any())
            {
                foreach (var cancionId in selectedSongs)
                {
                    var detallePlaylist = new DetallesPlaylist
                    {
                        PlaylistId = playlistId,
                        CancionId = cancionId
                    };
                    _context.DetallesPlaylist.Add(detallePlaylist);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
