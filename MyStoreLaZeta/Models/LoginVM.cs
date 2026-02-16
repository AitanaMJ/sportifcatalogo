using System.ComponentModel.DataAnnotations;

namespace MyStoreLaZeta.Models
{
    public class LoginVM
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }


    }
}
