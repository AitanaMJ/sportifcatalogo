using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogoPro.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        [Required]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Description { get; set; }

        public string? ImageName { get; set; } 

        [NotMapped] 
        public IFormFile? ImageFile { get; set; }

        public ICollection<Product> Products { get; set; }

    }
}
