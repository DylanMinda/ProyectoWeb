namespace Spotify.MVC.ViewModels
{
    public class EditProfileViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string TipoUsuario { get; set; }
    }

}
