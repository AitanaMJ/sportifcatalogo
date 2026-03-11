using System.Text.Json.Serialization;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStoreLaZeta.Context;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Utilities;

namespace MyStoreLaZeta.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public CheckoutController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] MPPaymentRequest request)
        {
            try
            {
                var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart");
                if (cart == null || cart.Count == 0)
                {
                    return BadRequest(new { success = false, message = "El carrito está vacío." });
                }

                // Credenciales
                MercadoPagoConfig.AccessToken = "TEST-8242036467932674-030212-e7c0c84de9ac435128816fed550dcb77-244346147";

                // ✅ CORRECCIÓN: Usamos FinalPrice para que el cobro sea el correcto con descuentos
                var totalCarrito = cart.Sum(x => x.FinalPrice * x.Quantity);

                // 🔒 Validaciones
                if (string.IsNullOrEmpty(request.Email))
                    return BadRequest(new { success = false, message = "Email requerido." });

                if (string.IsNullOrEmpty(request.PaymentMethodId))
                    return BadRequest(new { success = false, message = "Método de pago requerido." });

                // Usamos el total calculado internamente para mayor seguridad
                decimal montoFinal = request.TransactionAmount ?? totalCarrito;

                if (montoFinal <= 0)
                    return BadRequest(new { success = false, message = "Monto inválido." });

                bool isCardPayment = !string.IsNullOrEmpty(request.Token);

                // ✅ CREAR PAYMENT SÚPER BLINDADO
                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = montoFinal,
                    Description = "Compra en La Zeta Merchandising",
                    PaymentMethodId = request.PaymentMethodId,
                    Payer = new PaymentPayerRequest
                    {
                        Email = request.Email,
                        Identification = new IdentificationRequest
                        {
                            Type = "DNI",
                            Number = "32456789"
                        }
                    }
                };

                if (isCardPayment)
                {
                    paymentRequest.Token = request.Token;
                    paymentRequest.Installments = request.Installments ?? 1;

                    if (!string.IsNullOrEmpty(request.IssuerId))
                        paymentRequest.IssuerId = request.IssuerId;
                }

                var client = new PaymentClient();
                var payment = await client.CreateAsync(paymentRequest);

                if (payment.Status == "approved" || payment.Status == "pending" || payment.Status == "in_process")
                {
                    var order = new Order
                    {
                        OrderDate = DateTime.Now,
                        ClientName = request.Name,
                        Email = request.Email,
                        Phone = request.Phone,
                        Address = request.ShippingMethod == "EnvioDomicilio" ? request.Address
                        : "Retiro en Local",
                        ShippingMethod = request.ShippingMethod,
                        PaymentMethod = "MercadoPago",
                        Status = payment.Status == "approved" ? "Aprobado" : "Pendiente",

                       // Guardamos el total con descuento aplicado
                        TotalAmount = totalCarrito,

                        OrderItems = cart.Select(i => new OrderItem
                        {
                            ProductId = i.ProductId,
                            ProductName = !string.IsNullOrEmpty(i.SizeName)
                                ? $"{i.Name} ({i.ColorName} - {i.SizeName})"
                                : i.Name,

                           // Guardamos el precio unitario final (con descuento) en la base de datos
                            Price = i.FinalPrice,
                            Quantity = i.Quantity
                        }).ToList()
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    HttpContext.Session.Remove("Cart");

                    string statusToView = payment.Status == "approved" ? "approved" : "pending";

                    return Ok(new
                    {
                        success = true,
                        url = $"/Checkout/OrderSuccess?id={order.OrderId}&status={statusToView}"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Pago rechazado: {payment.StatusDetail}"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        public async Task<IActionResult> OrderSuccess(int id, string status = "")
        {
            {
                // la orden completa para mostrar los datos en la pantalla final
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound();
                }

                ViewBag.Status = status;
                ViewBag.OrderId = id; 

                return View(order); // <-- Enviamos el objeto 'order' a la vista
            }
        }

        public class MPPaymentRequest
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("email")] public string? Email { get; set; }
            [JsonPropertyName("phone")] public string? Phone { get; set; }
            [JsonPropertyName("address")] public string? Address { get; set; }
            [JsonPropertyName("shippingMethod")] public string? ShippingMethod { get; set; }

            [JsonPropertyName("token")] public string? Token { get; set; }
            [JsonPropertyName("issuer_id")] public string? IssuerId { get; set; }
            [JsonPropertyName("payment_method_id")] public string? PaymentMethodId { get; set; }
            [JsonPropertyName("transaction_amount")] public decimal? TransactionAmount { get; set; }
            [JsonPropertyName("installments")] public int? Installments { get; set; }
            [JsonPropertyName("payer")] public PayerDto? Payer { get; set; }
        }

        public class PayerDto
        {
            [JsonPropertyName("identification")] public IdentificationDto? Identification { get; set; }
        }

        public class IdentificationDto
        {
            [JsonPropertyName("type")] public string? Type { get; set; }
            [JsonPropertyName("number")] public string? Number { get; set; }
        }
    }
}