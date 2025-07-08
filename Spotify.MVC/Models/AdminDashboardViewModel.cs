using Spotify.Modelos;

namespace Spotify.MVC.ViewModels
{
    public class AdminDashboardViewModel
    {
        public List<Usuario> Usuarios { get; set; }
        public List<Cancion> Canciones { get; set; }
        public List<Album> Albums { get; set; }
    }
}
