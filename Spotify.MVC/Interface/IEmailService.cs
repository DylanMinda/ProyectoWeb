namespace Spotify.MVC.Interface
{
    public interface IEmailService
    {
        Task enviarEmailRecuperacionContraseña(string email);
    }
}
