using System.ComponentModel.DataAnnotations;
namespace MyStoreLaZeta.Models
{
    public class UserVM
    {
        public int UserId { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string RepeatPassword { get; set; }
        
    }
}
