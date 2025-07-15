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
        public IActionResult Index()
        {
            var lista = CRUD<Cancion>.GetAll();
            List<string> mensajes = new List<string>();

            // Aquí verificamos si alguna canción ha alcanzado el límite de reproducciones
            foreach (var cancion in lista)
            {
            }

            if (mensajes.Count > 0)
            {
                ViewBag.Message = string.Join("<br />", mensajes);  // Muestra todos los mensajes de anuncio
            }

            return View(lista);
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
