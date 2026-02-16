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
        public string PaymentMethod { get; set; } = "Efectivo"; // Efectivo, Transferencia, MercadoPago
        public string ShippingMethod { get; set; } = "Retiro";  // Retiro, EnvioDomicilio

        // --- RESUMEN (Solo lectura) ---
        public List<CartItemVM> CartItems { get; set; } = new List<CartItemVM>();
        public decimal Total { get; set; }
    }
}