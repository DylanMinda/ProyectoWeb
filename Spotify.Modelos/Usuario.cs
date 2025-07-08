using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify.Modelos
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Contraseña { get; set; }
        public string TipoUsuario { get; set; } // Puede ser "Usuario", "Artista" o "Administrador"
        public DateTime FechaRegistro { get; set; } // Fecha en que el usuario se registró
        public virtual List<Album>? Albums { get; set; }
        public virtual List<Cancion>? Canciones { get; set; }
        public virtual List<Playlist>? Playlists { get; set; }
        public virtual Plan? Plan { get; set; } // Plan al que está suscrito el usuario
    }
}
