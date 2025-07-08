using System.ComponentModel.DataAnnotations;
namespace Spotify.API.DTO

{
    public class CancionSubir
    {
        [Required] public string Titulo { get; set; } = null!;
        [Required] public IFormFile Archivo { get; set; } = null!;
        [Required] public string Genero { get; set; } = null!;
        public TimeSpan Duracion { get; set; }
        [Required] public int ArtistaId { get; set; }
        [Required] public int AlbumId { get; set; }
    }
}
