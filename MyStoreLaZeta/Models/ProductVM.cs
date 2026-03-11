using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyStoreLaZeta.Entities;

namespace MyStoreLaZeta.Models
{
    public class ProductVM
    {
        public int ProductId { get; set; }
        public CategoryVM Category { get; set; }

        public List<SelectListItem> Categories { get; set; }
        [Required]

        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int Stock { get; set; }
        public string ImageName { get; set; } = null;

        public IFormFile? ImageFile { get; set; }

        public decimal CostPrice { get; set; }

        public int? Discount { get; set; }

        public List<ProductVariation> Variations { get; set; } = new List<ProductVariation>();

        
        public bool HasVariations { get; set; } = false;
    }

}
