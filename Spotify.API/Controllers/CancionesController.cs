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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCancion(int id, Cancion cancion)
        {
            if (id != cancion.Id)
            {
                return BadRequest(); // Si el ID no coincide, devolvemos un error 400.
            }

            var cancionExistente = await _context.Canciones.FindAsync(id);
            if (cancionExistente == null)
            {
                return NotFound(); // Si no encontramos la canción, devolvemos un error 404.
            }

            // Actualizamos solo los campos necesarios.
            cancionExistente.Titulo = cancion.Titulo;
            cancionExistente.Duracion = cancion.Duracion;
            cancionExistente.Genero = cancion.Genero;
            cancionExistente.ArtistaId = cancion.ArtistaId;
            cancionExistente.AlbumId = cancion.AlbumId;

            _context.Entry(cancionExistente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync(); // Guardamos los cambios en la base de datos.
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CancionExists(id))
                {
                    return NotFound(); // Si la canción no existe, devolvemos un error 404.
                }
                else
                {
                    throw; // Lanzamos cualquier otro error de concurrencia.
                }
            }

            return NoContent(); // Si todo va bien, devolvemos una respuesta sin contenido.
        }

        // POST: api/Canciones
        [HttpPost]
        public async Task<ActionResult<Cancion>> PostCancion(Cancion cancion)
        {
            _context.Canciones.Add(cancion); // Añadimos la nueva canción a la base de datos.
            await _context.SaveChangesAsync(); // Guardamos los cambios.

            return CreatedAtAction("GetCancion", new { id = cancion.Id }, cancion); // Devolvemos la canción creada.
        }

        // DELETE: api/Canciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCancion(int id)
        {
            var cancion = await _context.Canciones.FindAsync(id);
            if (cancion == null)
            {
                return NotFound(); // Si no encontramos la canción, devolvemos un error 404.
            }

            _context.Canciones.Remove(cancion); // Eliminamos la canción de la base de datos.
            await _context.SaveChangesAsync(); // Guardamos los cambios.

            return NoContent(); // Devolvemos una respuesta sin contenido para indicar que la operación fue exitosa.
        }

        private bool CancionExists(int id)
        {
            return _context.Canciones.Any(e => e.Id == id); // Verificamos si la canción existe en la base de datos.
        }

        // POST: api/Canciones/upload
        [HttpPost("upload")]
        public async Task<ActionResult<Cancion>> UploadCancion([FromForm] CancionDTO dto)
        {
            // 1. Obtenemos o creamos el contenedor de almacenamiento
            var container = _blobService.GetBlobContainerClient(_containerName);
            await container.CreateIfNotExistsAsync();

            // 2. Generamos un nombre único para el archivo y lo subimos
            var ext = Path.GetExtension(dto.Archivo.FileName);
            var blobName = $"{Guid.NewGuid()}{ext}";
            var blob = container.GetBlobClient(blobName);
            await using var stream = dto.Archivo.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = dto.Archivo.ContentType });

            // 3. Mapear el DTO a la entidad Cancion y guardarla en la base de datos
            var cancion = new Cancion
            {
                Titulo = dto.Titulo,
                ArchivoUrl = blob.Uri.ToString(),
                Duracion = dto.Duracion,
                Genero = dto.Genero,
                ArtistaId = dto.ArtistaId,
                AlbumId = dto.AlbumId
            };

            _context.Canciones.Add(cancion); // Añadimos la canción a la base de datos.
            await _context.SaveChangesAsync(); // Guardamos los cambios.

            return CreatedAtAction(nameof(GetCancion), new { id = cancion.Id }, cancion); // Devolvemos la canción creada.
        }

    }
}
