using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.ViewModels;

namespace Spotify.MVC.Controllers
{
    public class CancionesController : Controller
    {
        public IActionResult Index()
        {
            var lista = CRUD<Cancion>.GetAll();
            return View(lista);
        }
        
        [HttpGet]
        public IActionResult Subir() => View(new CancionUploadViewModel());

        [HttpPost]
        public IActionResult Subir(CancionUploadViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // abre el stream y llama al CRUD
            var cancion = CRUD<Cancion>.UploadWithFile(
                vm.Titulo,
                vm.Archivo.OpenReadStream(),
                vm.Archivo.FileName,
                vm.Archivo.ContentType,
                vm.Genero,
                vm.Duracion,
                vm.ArtistaId,
                vm.AlbumId
            );

            // redirige o maneja la respuesta
            return RedirectToAction("Index");
        }

    }
}
