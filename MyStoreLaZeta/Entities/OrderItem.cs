using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyStoreLaZeta.Entities
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // Agregamos esto para guardar el nombre en el momento de la compra
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")] // Recomendado para precios
        public decimal Price { get; set; }
    }
}