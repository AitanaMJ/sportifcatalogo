using System.ComponentModel.DataAnnotations;

namespace CatalogoPro.Entities
{
    public class User
    {
        public int UserId { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Email{ get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Type { get; set; }

        // Token para recuperar contraseña
        public string? ResetToken { get; set; }

        // Fecha de expiración del token
        public DateTime? ResetTokenExpires { get; set; }

    }
}
