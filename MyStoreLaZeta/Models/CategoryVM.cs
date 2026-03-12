using System.ComponentModel.DataAnnotations;

namespace MyStoreLaZeta.Models
{
    public class CategoryVM
    {
        public int CategoryId { get; set; }
        [Required]


        public string Name { get; set; }


        
        public bool IsActive { get; set; }

        
        public string? Description { get; set; }

        public string? ImageName { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
