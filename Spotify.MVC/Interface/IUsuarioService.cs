using Spotify.Modelos;

namespace Spotify.MVC.Interface
{
    public interface IUsuarioService
    {
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario> CrearUsuarioAsync(Usuario usuario);
        Task<Usuario?> ValidarUsuarioAsync(string email, string contraseña);
        Task<Usuario?> ObtenerUsuarioPorIdAsync(int id);
    }
}
