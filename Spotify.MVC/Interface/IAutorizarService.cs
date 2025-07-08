namespace Spotify.MVC.Interface
{
    public interface IAutorizarService
    {
        Task<bool> Login(string email, string contraseña);
        Task<bool> Register(string email, string nombre, string contraseña);
    }
}
