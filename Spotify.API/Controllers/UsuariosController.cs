using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using Spotify.Modelos;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spotify.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        // El constructor recibe una instancia de AppDbContext, que se usa para interactuar con la base de datos.
        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // Método GET para obtener todos los usuarios, incluyendo su plan asociado (si existe).
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            // Aquí estamos incluyendo el plan asociado a cada usuario, usando el método Include.
            var usuarios = await _context.Usuarios
                .Include(u => u.Plan)
                .ToListAsync(); // Se convierte la consulta en una lista de usuarios.

            return usuarios;
        }

        // Método GET para obtener un usuario específico mediante su ID.
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            // Buscamos al usuario en la base de datos usando el ID proporcionado.
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                // Si no encontramos el usuario, devolvemos un error 404.
                return NotFound();
            }

            // Si el usuario existe, devolvemos el usuario.
            return usuario;
        }

        // Método PUT para actualizar los datos de un usuario específico.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            // Verificamos si el ID del usuario coincide con el proporcionado.
            if (id != usuario.Id)
            {
                return BadRequest(); // Si no coinciden, devolvemos un error 400.
            }

            // Buscamos el usuario existente en la base de datos.
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                // Si no encontramos al usuario, devolvemos un error 404.
                return NotFound();
            }

            // Verificamos si la contraseña fue cambiada. Si es así, la hasheamos antes de guardarla.
            if (usuario.Contraseña != usuarioExistente.Contraseña &&
                !BCrypt.Net.BCrypt.Verify(usuario.Contraseña, usuarioExistente.Contraseña))
            {
                // Si la contraseña es nueva, la encriptamos (hasheamos).
                usuario.Contraseña = BCrypt.Net.BCrypt.HashPassword(usuario.Contraseña);
            }
            else
            {
                // Si no hay cambios en la contraseña, mantenemos la anterior.
                usuario.Contraseña = usuarioExistente.Contraseña;
            }

            // Actualizamos los valores del usuario existente con los nuevos valores proporcionados.
            _context.Entry(usuarioExistente).CurrentValues.SetValues(usuario);

            try
            {
                // Intentamos guardar los cambios en la base de datos.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Si ocurre un error de concurrencia, verificamos si el usuario aún existe.
                if (!UsuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw; // Si no es un error de concurrencia, lo lanzamos nuevamente.
                }
            }

            // Si todo sale bien, devolvemos una respuesta exitosa sin contenido.
            return NoContent();
        }

        // Método POST para crear un nuevo usuario.
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            // Antes de guardar el usuario, aseguramos que la contraseña esté hasheada.
            usuario.Contraseña = BCrypt.Net.BCrypt.HashPassword(usuario.Contraseña);

            // Añadimos el usuario a la base de datos.
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Devolvemos una respuesta indicando que el usuario ha sido creado correctamente.
            return CreatedAtAction("GetUsuario", new { id = usuario.Id }, usuario);
        }

        // Método DELETE para eliminar un usuario específico.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            // Buscamos al usuario en la base de datos.
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                // Si no encontramos al usuario, devolvemos un error 404.
                return NotFound();
            }

            // Eliminamos el usuario de la base de datos.
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            // Devolvemos una respuesta sin contenido para indicar que la operación fue exitosa.
            return NoContent();
        }

        [HttpGet("existe-email/{email}")]
        public async Task<ActionResult<bool>> ExisteEmail(string email)
        {
            // Verificamos si existe algún usuario con el email proporcionado
            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.Email == email);

            // Devolvemos true si el email existe, false si no existe
            return Ok(emailExiste);
        }

        // Método auxiliar para verificar si un usuario con el ID especificado existe.
        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
