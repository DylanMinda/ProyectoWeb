namespace Spotify.MVC.Interface
{
    public interface IEmailService
    {
        Task enviarEmailBienvenida(string email); 
        Task enviarEmailRecuperacionContraseña(string email);
    }
}
