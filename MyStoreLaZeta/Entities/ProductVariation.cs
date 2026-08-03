using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogoPro.Entities
{
    public class ProductVariation
    {
        [Key]
        public int Id { get; set; }

      
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        
        public string Color { get; set; } 
        public string Size { get; set; }  

        
        public int Stock { get; set; }
    }
}
