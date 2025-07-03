using Microsoft.AspNetCore.Authentication;
using Spotify.APIConsumer;
using Spotify.Modelos;
using System.Security.Claims;
using Spotify.MVC.Interface;

namespace Spotify.MVC.Services
{
    public class AutorizarService: IAutorizarService
    {
        private readonly IHttpContextAccessor _httpContextAccessor; 
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

                    Console.WriteLine($"Comparadno pas ingrasado {contraseña} con contraseña guardada {usuario.Contraseña} ");
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

                        await _httpContextAccessor.HttpContext.SignInAsync("Cookies", usuarioAutenticado);
                        return true;
                    }
                }
            }
            return false; 


        }

        public async Task<bool> Register(string email, string nombre, string contraseña)
        {
            var usuarioExistente = CRUD<Usuario>.GetAll()
                .FirstOrDefault(u => u.Email == email);
            if (usuarioExistente != null)
            {
                return false; // El usuario ya existe   
            }

            try
            {
                CRUD<Usuario>.Create(new Usuario
                {
                    Id = 0,
                    Email = email,
                    Contraseña = contraseña, 
                    Nombre = nombre,
                    TipoUsuario = "cliente",
                    FechaRegistro = DateTime.UtcNow
                });
                return true; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar usuario: {ex.Message}");
                return false; 

            }
        }
    }
}
