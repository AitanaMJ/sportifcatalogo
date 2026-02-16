using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyStoreLaZeta.Entities
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // --- DATOS DEL CLIENTE (INVITADO) ---
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // --- DATOS DE LA VENTA ---
        public decimal TotalAmount { get; set; }

        // Aquí guardamos si es "Efectivo" o "MercadoPago"
        public string PaymentMethod { get; set; } = "Efectivo";

        // Aquí guardamos si es "Retiro" o "Envío" (LO QUE PEDISTE)
        public string ShippingMethod { get; set; } = "Retiro";

        public string Status { get; set; } = "Pendiente";

        // --- RELACIONES ---

        // El usuario es opcional (int?) para permitir compras sin loguearse
        public int? UserId { get; set; }
        public User? User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}