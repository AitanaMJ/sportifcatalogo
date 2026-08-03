using System.ComponentModel.DataAnnotations;

namespace CatalogoPro.Models
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordVM
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        public string NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }
    }
}