using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.ViewModels;

namespace Spotify.MVC.Controllers
{
    public class CancionesController : Controller
    {
        private readonly AppDbContext _context;
        public CancionesController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var lista = CRUD<Cancion>.GetAll();
            return View(lista);
        }

        // GET: Cancion/Player/5
        public async Task<IActionResult> Player(int? id)
        {
            var cancion = _context.Canciones.Find(id);
            if (cancion == null)
            {
                return NotFound();
            }

            return View(cancion);
        }

        // Método opcional para obtener la siguiente canción de una playlist
        public async Task<IActionResult> NextSong(int playlistId, int currentSongId)
        {
            var playlist = await _context.Playlists
                .Include(p => p.DetallesPlaylists)
                .ThenInclude(d => d.Cancion)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return NotFound();

            var canciones = playlist.DetallesPlaylists.Select(d => d.Cancion).ToList();
            var currentIndex = canciones.FindIndex(c => c.Id == currentSongId);

            if (currentIndex == -1 || currentIndex >= canciones.Count - 1)
                return NotFound();

            var nextSong = canciones[currentIndex + 1];
            return RedirectToAction("Player", new { id = nextSong.Id });
        }

        // Método opcional para obtener la canción anterior de una playlist
        public async Task<IActionResult> PreviousSong(int playlistId, int currentSongId)
        {
            var playlist = await _context.Playlists
                .Include(p => p.DetallesPlaylists)
                .ThenInclude(d => d.Cancion)
                .FirstOrDefaultAsync(p => p.Id == playlistId);

            if (playlist == null)
                return NotFound();

            var canciones = playlist.DetallesPlaylists.Select(d => d.Cancion).ToList();
            var currentIndex = canciones.FindIndex(c => c.Id == currentSongId);

            if (currentIndex <= 0)
                return NotFound();

            var previousSong = canciones[currentIndex - 1];
            return RedirectToAction("Player", new { id = previousSong.Id });
        }


        [HttpGet]
        public IActionResult Subir()
        {
            // Obtener el ID del artista logueado
            var artistaId = User.FindFirst("ArtistaId")?.Value;
            var todosLosAlbums = CRUD<Album>.GetAll();
            var albumsDelArtista = todosLosAlbums
                .Where(a => a.ArtistaId.ToString() == artistaId)
                .ToList();

            ViewBag.Albums = new SelectList(albumsDelArtista, "Id", "Nombre");
            return View(new CancionSubirViewModel());
        }

        [HttpPost]
        public IActionResult Subir(CancionSubirViewModel cancionviewmodel)
        {
            // Obtener el ID del artista desde el usuario logueado
            var artistaId = User.FindFirst("ArtistaId")?.Value;
            cancionviewmodel.ArtistaId = int.Parse(artistaId);

            if (!ModelState.IsValid) return View(cancionviewmodel);

            // abre el stream y llama al CRUD
            var cancion = CRUD<Cancion>.UploadWithFile(
                cancionviewmodel.Titulo,
                cancionviewmodel.Archivo.OpenReadStream(),
                cancionviewmodel.Archivo.FileName,
                cancionviewmodel.Archivo.ContentType,
                cancionviewmodel.Genero,
                cancionviewmodel.Duracion,
                cancionviewmodel.ArtistaId,
                cancionviewmodel.AlbumId
            );

            // redirige o maneja la respuesta
            return RedirectToAction("Index");
        }

    }
}
