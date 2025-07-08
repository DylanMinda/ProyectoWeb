using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify.Modelos
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int UsuarioId { get; set; } // ID del usuario que creó la playlist
        public virtual List<DetallesPlaylist>? DetallesPlaylists { get; set; } // Lista de canciones en la playlist
        public Usuario? Creador { get; set; } // Usuario que creó la playlist
    }
}
