namespace MyStoreLaZeta.Models
{
    public class CartItemVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; }

        public string ImageName { get; set; }

        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public decimal CostPrice { get; set; } // Precio original
        public int Discount { get; set; }  // Porcentaje de descuento

        // Propiedad calculada para obtener el precio real con descuento aplicado
        public decimal FinalPrice => Discount > 0
            ? Price - (Price * (decimal)Discount / 100)
            : Price;

        public int? VariationId { get; set; } // ID de la combinación Color/Talle
        public string ColorName { get; set; }
        public string SizeName { get; set; }
    }
}
