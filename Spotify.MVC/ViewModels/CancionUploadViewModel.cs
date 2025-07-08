using System.ComponentModel.DataAnnotations;
namespace Spotify.MVC.ViewModels

{
    public class CancionUploadViewModel
    {
        [Required] public string Titulo { get; set; } = null!;
        [Required] public IFormFile Archivo { get; set; } = null!;
        public TimeSpan Duracion { get; set; }
        [Required] public string Genero { get; set; } = null!;
        [Required] public int ArtistaId { get; set; }
        [Required] public int AlbumId { get; set; }
    }
}
