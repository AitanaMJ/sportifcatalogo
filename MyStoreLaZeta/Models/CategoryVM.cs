using System.ComponentModel.DataAnnotations;

namespace MyStoreLaZeta.Models
{
    public class CategoryVM
    {
        public int CategoryId { get; set; }
        [Required]


        public string Name { get; set; } 
    }
}
