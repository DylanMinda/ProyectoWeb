namespace Spotify.Modelos
{
    public class Album
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Genero { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public int ArtistaId { get; set; } // ID del artista que lanzó el álbum
        public virtual Usuario? Artista { get; set; } // Usuario que creó el álbum
        public virtual List<Cancion>? Canciones { get; set; } = new List<Cancion>();      

    }
}
