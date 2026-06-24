using System.Text.Json.Serialization;
using System.Security.Claims; 
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

                MercadoPagoConfig.AccessToken = _configuration["MercadoPago:AccessToken"];

                if (string.IsNullOrEmpty(MercadoPagoConfig.AccessToken))
                {
                    throw new Exception("Error de configuración: No se encontró el token de Mercado Pago en el servidor.");
                }

                var totalCarrito = cart.Sum(x => x.FinalPrice * x.Quantity);

                if (string.IsNullOrEmpty(request.Email))
                    return BadRequest(new { success = false, message = "Email requerido." });

                if (string.IsNullOrEmpty(request.PaymentMethodId))
                    return BadRequest(new { success = false, message = "Método de pago requerido." });

                decimal montoFinal = request.TransactionAmount ?? totalCarrito;

                if (montoFinal <= 0)
                    return BadRequest(new { success = false, message = "Monto inválido." });

                bool isCardPayment = !string.IsNullOrEmpty(request.Token);

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
                            // Tomamos el DNI del request. Si viene vacío, usamos uno por defecto para no romper el código.
                            Type = request.Payer?.Identification?.Type ?? "DNI",
                            Number = request.Payer?.Identification?.Number ?? "00000000"
                        }
                    }
                };

                if (isCardPayment)
                    // ... el resto de tu código sigue igual

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
                    // RECUPERAMOS EL ID DEL USUARIO SI INICIÓ SESIÓN
                    int? currentUserId = null;
                    if (User.Identity != null && User.Identity.IsAuthenticated)
                    {
                        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (int.TryParse(userIdClaim, out int id))
                        {
                            currentUserId = id;
                        }
                    }

                    //INICIAMOS LA TRANSACCIÓN SQL (Para proteger el stock)
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        foreach (var item in cart)
                        {
                            if (item.VariationId.HasValue && item.VariationId > 0)
                            {
                                var filasAfectadasVariacion = await _context.ProductVariations
                                    .Where(v => v.Id == item.VariationId && v.Stock >= item.Quantity)
                                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock - item.Quantity));

                                if (filasAfectadasVariacion == 0)
                                    throw new Exception($"Lo sentimos, alguien compró {item.Name} justo antes que tú y nos quedamos sin stock en ese talle/color.");

                                await _context.Products
                                    .Where(p => p.ProductId == item.ProductId)
                                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));
                            }
                            else
                            {
                                var filasAfectadasProducto = await _context.Products
                                    .Where(p => p.ProductId == item.ProductId && p.Stock >= item.Quantity)
                                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

                                if (filasAfectadasProducto == 0)
                                    throw new Exception($"Lo sentimos, alguien compró {item.Name} justo antes que tú y nos quedamos sin stock.");
                            }
                        }

                        // 3. CREAMOS LA ORDEN NORMALMENTE
                        var order = new Order
                        {
                            UserId = currentUserId, // <-- ACÁ ASIGNAMOS EL ID DINÁMICO
                            OrderDate = DateTime.Now,
                            ClientName = request.Name,
                            Email = request.Email,
                            Phone = request.Phone,
                            Address = request.ShippingMethod == "EnvioDomicilio" ? request.Address : "Retiro en Local",
                            ShippingMethod = request.ShippingMethod,
                            PaymentMethod = "MercadoPago",
                            Status = payment.Status == "approved" ? "Aprobado" : "Pendiente",
                            TotalAmount = totalCarrito,
                            OrderItems = cart.Select(i => new OrderItem
                            {
                                ProductId = i.ProductId,
                                ProductName = !string.IsNullOrEmpty(i.SizeName)
                                    ? $"{i.Name} ({i.ColorName} - {i.SizeName})"
                                    : i.Name,
                                Price = i.FinalPrice,
                                Quantity = i.Quantity
                            }).ToList()
                        };

                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();

                        // 4. CONFIRMAMOS LA TRANSACCIÓN SI TODO SALIÓ BIEN
                        await transaction.CommitAsync();

                        HttpContext.Session.Remove("Cart");

                        string statusToView = payment.Status == "approved" ? "approved" : "pending";

                        // Extraemos el link del cupón (solo vendrá con datos si es pago en efectivo)
                        string ticketUrl = payment.TransactionDetails?.ExternalResourceUrl ?? "";

                        return Ok(new
                        {
                            success = true,
                            // Agregamos el ticket a la redirección
                            url = $"/Checkout/OrderSuccess?id={order.OrderId}&status={statusToView}&ticket={Uri.EscapeDataString(ticketUrl)}"
                        });
                    }
                    catch (Exception)
                    {
                        // SI EXPLOTA ALGO, DESHACEMOS LA VENTA PARA NO PERDER STOCK
                        await transaction.RollbackAsync();
                        throw;
                    }
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

        // Agregamos el parámetro 'ticket' a la firma del método
        public async Task<IActionResult> OrderSuccess(int id, string status = "", string ticket = "")
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.Status = status;
            ViewBag.OrderId = id;

            // Desencriptamos la URL del ticket y la guardamos en el ViewBag para que la vista la lea
            ViewBag.TicketUrl = string.IsNullOrEmpty(ticket) ? "" : Uri.UnescapeDataString(ticket);

            return View(order);
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