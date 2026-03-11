using System.ComponentModel.DataAnnotations;

namespace MyStoreLaZeta.Entities
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageName { get; set; } = null;

        public Category? Category { get; set; }

        public bool HasVariations { get; set; } = false;

        public decimal CostPrice { get; set; } 
        public int Discount { get; set; }

        public virtual ICollection<ProductVariation> Variations { get; set; } = new List<ProductVariation>();
    }
}
