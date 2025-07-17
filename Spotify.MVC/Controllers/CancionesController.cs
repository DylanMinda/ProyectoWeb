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
            if (id == null)
            {
                return NotFound("ID de canción no proporcionado");
            }

            var cancion = await _context.Canciones
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cancion == null)
            {
                return NotFound($"No se encontró la canción con ID: {id}");
            }

            return View(cancion);
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
