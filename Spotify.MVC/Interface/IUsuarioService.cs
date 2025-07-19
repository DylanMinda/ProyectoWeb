using Spotify.Modelos;

namespace Spotify.MVC.Interface
{
    public interface IUsuarioService
    {
        Task<bool> exiteEmail(string email);
        Task<Usuario> crearUsuario(Usuario usuario);
        Task<Usuario?> validarUsuario(string email, string contraseña);
        Task<Usuario?> nombrePorId(int id);
    }
}
