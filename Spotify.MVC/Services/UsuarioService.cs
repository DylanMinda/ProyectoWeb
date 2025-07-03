using Spotify.Modelos;
using Spotify.MVC.Interface;
using System.Security.Cryptography;
using System.Text;
namespace Spotify.MVC.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public UsuarioService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001/api";
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/usuarios/existe-email/{email}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }

        public async Task<Usuario> CrearUsuarioAsync(Usuario usuario)
        {
            // Encriptar contraseña antes de enviar
            usuario.Contraseña = EncriptarContraseña(usuario.Contraseña);
            usuario.FechaRegistro = DateTime.UtcNow;

            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/usuarios", usuario);
            response.EnsureSuccessStatusCode();

            var usuarioCreado = await response.Content.ReadFromJsonAsync<Usuario>();
            return usuarioCreado!;
        }

        public async Task<Usuario?> ValidarUsuarioAsync(string email, string contraseña)
        {
            var contraseñaEncriptada = EncriptarContraseña(contraseña);
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/usuarios/validar",
                new { Email = email, Contraseña = contraseñaEncriptada });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Usuario>();
            }
            return null;
        }

        public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/usuarios/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Usuario>();
            }
            return null;
        }

        private string EncriptarContraseña(string contraseña)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(contraseña));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
