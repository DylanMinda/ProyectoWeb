using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify.Modelos
{
    public class Cancion
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public TimeSpan Duracion { get; set; }
        public string Genero { get; set; }
        public int ArtistaId { get; set; } // ID del artista que interpreta la canción
        public int AlbumId { get; set; } // ID del álbum al que pertenece la canción
        public virtual Album? Album { get; set; }
        public virtual List<DetallesPlaylist>? DetallesPlaylist { get; set; }
    }
}