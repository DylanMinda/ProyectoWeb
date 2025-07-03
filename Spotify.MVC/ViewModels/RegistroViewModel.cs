using System.ComponentModel.DataAnnotations;

namespace Spotify.MVC.ViewModels
{
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contraseña { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma tu contraseña")]
        [DataType(DataType.Password)]
        [Compare("Contraseña", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContraseña { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un tipo de usuario")]
        [Display(Name = "Tipo de usuario")]
        public string TipoUsuario { get; set; } = "Usuario";
    }
}
