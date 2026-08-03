using System.ComponentModel.DataAnnotations;

namespace CatalogoPro.Models
{
    public class LoginVM
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }


    }
}
