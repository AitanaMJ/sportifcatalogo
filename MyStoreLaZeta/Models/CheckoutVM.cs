namespace MyStoreLaZeta.Models
{
    public class CheckoutVM
    {
        // --- DATOS DEL CLIENTE ---
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // --- OPCIONES DE LA COMPRA ---
        public string PaymentMethod { get; set; } = "Efectivo"; // Efectivo, trransferencia, MercadoPago
        public string ShippingMethod { get; set; } = "Retiro";  // rtiro, EnvioDomicilio

        // --- RESUMEN  ---
        public List<CartItemVM> CartItems { get; set; } = new List<CartItemVM>();
        public decimal Total { get; set; }
    }
}