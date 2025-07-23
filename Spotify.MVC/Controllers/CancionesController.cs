using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.ViewModels;
using System.Security.Claims;

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
            if (!id.HasValue) return NotFound();

            var cancion = await _context.Canciones
                .Include(c => c.ArtistaCodigoNav)
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (cancion == null) return NotFound();

            // Obtener información del usuario actual
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios
                .Include(u => u.Plan)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null) return RedirectToAction("Index", "Login");

            // Verificar si es plan gratuito (precio 0 o null)
            bool esPlanGratuito = usuario.Plan?.PrecioMensual == 0 || usuario.Plan?.PrecioMensual == null;

            // Si es plan gratuito, verificar el límite de reproducciones
            if (esPlanGratuito)
            {
                var sessionKey = $"reproducciones_cancion_{id.Value}";
                var reproducciones = HttpContext.Session.GetInt32(sessionKey) ?? 0;

                if (reproducciones >= 3) // Límite de 3 reproducciones gratuitas
                {
                    TempData["ErrorMessage"] = "Has alcanzado el límite de reproducciones gratuitas para esta canción. Upgrade a Premium para escuchar sin límites.";
                    return RedirectToAction("Index", "Planes");
                }

                // Incrementar contador de reproducciones
                HttpContext.Session.SetInt32(sessionKey, reproducciones + 1);
                ViewBag.ReproduccionesRestantes = 3 - (reproducciones + 1);
            }

            ViewBag.EsPlanGratuito = esPlanGratuito;
            ViewBag.PuedeDescargar = !esPlanGratuito; // Solo usuarios premium pueden descargar
            ViewBag.Usuario = usuario;

            return View(cancion);
        }


        // Acción para pausar automáticamente (para usuarios gratuitos)
        [HttpPost]
        public IActionResult PausarReproduccion(int cancionId, int tiempoTranscurrido)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = _context.Usuarios.Include(u => u.Plan).FirstOrDefault(u => u.Id == usuarioId);

            if (usuario == null) return Json(new { success = false });

            bool esPlanGratuito = usuario.Plan?.PrecioMensual == 0 || usuario.Plan?.PrecioMensual == null;

            // Para usuarios gratuitos: pausar cada 30 segundos (puedes ajustar este tiempo)
            if (esPlanGratuito && tiempoTranscurrido >= 30)
            {
                return Json(new
                {
                    success = true,
                    pausar = true,
                    mensaje = "Pausa publicitaria - Upgrade a Premium para música sin interrupciones"
                });
            }

            return Json(new { success = true, pausar = false });
        }


        // Verificar estado de reproducciones restantes
        [HttpGet]
        public IActionResult VerificarLimiteReproducciones(int cancionId)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = _context.Usuarios.Include(u => u.Plan).FirstOrDefault(u => u.Id == usuarioId);

            if (usuario == null) return Json(new { success = false });

            bool esPlanGratuito = usuario.Plan?.PrecioMensual == 0 || usuario.Plan?.PrecioMensual == null;

            if (!esPlanGratuito)
            {
                return Json(new { success = true, limitado = false });
            }

            var sessionKey = $"reproducciones_cancion_{cancionId}";
            var reproducciones = HttpContext.Session.GetInt32(sessionKey) ?? 0;
            var restantes = Math.Max(0, 3 - reproducciones);

            return Json(new
            {
                success = true,
                limitado = true,
                reproducciones = reproducciones,
                restantes = restantes,
                bloqueado = reproducciones >= 3
            });
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
