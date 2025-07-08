using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using System.Security.Claims;

namespace Spotify.MVC.Services
{
    public class AutorizarService : IAutorizarService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Inyectar IHttpContextAccessor en el constructor
        public AutorizarService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Login(string email, string contraseña)
        {
            var usuarios = CRUD<Usuario>.GetAll();
            foreach (var usuario in usuarios)
            {
                if (usuario.Email == email)
                {
                    Console.WriteLine($"Comparando contraseña ingresada {contraseña} con contraseña guardada {usuario.Contraseña}");

                    if (BCrypt.Net.BCrypt.Verify(contraseña, usuario.Contraseña))
                    {
                        var datosUsuario = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, usuario.Nombre),
                            new Claim(ClaimTypes.Email, usuario.Email),
                            new Claim("TipoUsuario", usuario.TipoUsuario)
                        };

                        var credencialDigital = new ClaimsIdentity(datosUsuario, "Cookies");
                        var usuarioAutenticado = new ClaimsPrincipal(credencialDigital);

                        // Usar _httpContextAccessor para acceder al contexto de la sesión
                        await _httpContextAccessor.HttpContext.SignInAsync("Cookies", usuarioAutenticado);
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<bool> Register(string email, string nombre, string contraseña)
        {
            if (string.IsNullOrEmpty(contraseña))
            {
                return false; // La contraseña no puede ser vacía
            }

            var usuarioExistente = CRUD<Usuario>.GetAll()
                .FirstOrDefault(u => u.Email == email);

            if (usuarioExistente != null)
            {
                return false; // El usuario ya existe
            }

            try
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(contraseña);

                var nuevoUsuario = new Usuario
                {
                    Nombre = nombre,
                    Email = email,
                    Contraseña = hashedPassword,
                    TipoUsuario = "Cliente", // O el tipo de usuario que quieras asignar
                    FechaRegistro = DateTime.UtcNow
                };

                CRUD<Usuario>.Create(nuevoUsuario);
                return true; // Registro exitoso
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar usuario: {ex.Message}");
                return false; // Si ocurre un error
            }
        }
    }
}
