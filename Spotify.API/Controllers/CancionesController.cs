using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.API.DTO;
using Spotify.Modelos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spotify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancionesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BlobServiceClient _blobService;
        private readonly string _containerName;

        public CancionesController(
            AppDbContext context,
            BlobServiceClient blobService,
            IConfiguration config)
        {
            _context = context;
            _blobService = blobService;
            _containerName = config["AzureStorage:ContainerName"]!;
        }

        // GET: api/Canciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cancion>>> GetCanciones()
        {
            return await _context.Canciones.ToListAsync();
        }

        // GET: api/Canciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cancion>> GetCancion(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);

            if (cancion == null)
            {
                return NotFound();
            }

            return cancion;
        }

        // PUT: api/Canciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCancion(int id, Cancion cancion)
        {
            if (id != cancion.Id)
            {
                return BadRequest();
            }

            // Verificamos que la canción existe
            var cancionExistente = await _context.Canciones.FindAsync(id);
            if (cancionExistente == null)
            {
                return NotFound();
            }

            // Si se desea actualizar la propiedad TotalReproducciones, no la actualizamos aquí, ya que se maneja automáticamente.
            // Solo actualizamos los campos necesarios, por ejemplo, Titulo, Duracion, etc.

            cancionExistente.Titulo = cancion.Titulo;
            cancionExistente.Duracion = cancion.Duracion;
            cancionExistente.Genero = cancion.Genero;
            cancionExistente.ArtistaId = cancion.ArtistaId;
            cancionExistente.AlbumId = cancion.AlbumId;

            _context.Entry(cancionExistente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CancionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        // POST: api/Canciones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Cancion>> PostCancion(Cancion cancion)
        {
            _context.Canciones.Add(cancion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCancion", new { id = cancion.Id }, cancion);
        }

        // DELETE: api/Canciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCancion(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
            {
                return NotFound();
            }

            _context.Canciones.Remove(cancion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CancionExists(int id)
        {
            return _context.Canciones.Any(e => e.Id == id);
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Cancion>> UploadCancion([FromForm] CancionDTO dto)
        {
            // 1. Obtener/crear contenedor
            var container = _blobService.GetBlobContainerClient(_containerName);
            await container.CreateIfNotExistsAsync();

            // 2. Generar nombre único y subir
            var ext = Path.GetExtension(dto.Archivo.FileName);
            var blobName = $"{Guid.NewGuid()}{ext}";
            var blob = container.GetBlobClient(blobName);
            await using var stream = dto.Archivo.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = dto.Archivo.ContentType });

            // 3. Mapear DTO → entidad y guardar en BD
            var cancion = new Cancion
            {
                Titulo = dto.Titulo,
                ArchivoUrl = blob.Uri.ToString(),
                Duracion = dto.Duracion,
                Genero = dto.Genero,
                ArtistaId = dto.ArtistaId,
                AlbumId = dto.AlbumId
            };

            _context.Canciones.Add(cancion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCancion), new { id = cancion.Id }, cancion);
        }

        [HttpPost("{id}/incrementar-reproduccion")]
        public async Task<IActionResult> IncrementarReproduccion(int id)
        {
            // Obtener el usuario logueado
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Obtener el usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                return Unauthorized(); // Si no está logueado
            }

            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
            {
                return NotFound();
            }

            // Si el usuario tiene el plan Free, aplicar las restricciones
            if (usuario.Plan.Nombre == "Free")
            {
                if (cancion.TotalReproducciones >= 5) // Límite de 5 reproducciones
                {
                    // Mostrar el anuncio
                    return BadRequest("Límite de reproducciones alcanzado. Cambia de plan o espera.");
                }

                // Si no se alcanzó el límite, incrementar la reproducción
                cancion.TotalReproducciones++;

                // Guardamos los cambios
                _context.Canciones.Update(cancion);
                await _context.SaveChangesAsync();

                // Respuesta con anuncio
                return Ok("Reproducción exitosa, mostrando anuncio...");
            }

            // Si es Premium, simplemente incrementa las reproducciones sin restricciones
            cancion.TotalReproducciones++;
            _context.Canciones.Update(cancion);
            await _context.SaveChangesAsync();

            return Ok("Reproducción exitosa sin restricciones.");
        }


    }
}
