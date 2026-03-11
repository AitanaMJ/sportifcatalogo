using System.ComponentModel.DataAnnotations;

namespace MyStoreLaZeta.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        [Required]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; }

    }
}
