using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStoreLaZeta.Context;
using MyStoreLaZeta.Entities;

namespace MyStoreLaZeta.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> MyOrders()
        {
            // Buscamos el claim del email, que es el que coincide con la BD
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Si por alguna razón el claim es nulo, podrías intentar buscar al usuario en la BD 
            // por su nombre para obtener su mail, pero lo ideal es que el claim esté presente.
            if (string.IsNullOrEmpty(userEmail))
            {
                return View(new List<Order>());
            }

            var pedidos = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Email.ToLower() == userEmail.ToLower())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pedidos);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageOrders(DateTime? desde, DateTime? hasta)
        {
            var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();

            if (desde.HasValue && hasta.HasValue)
            {
                // Si el usuario buscó en el historial, traemos ese rango exacto
                query = query.Where(o => o.OrderDate >= desde.Value && o.OrderDate < hasta.Value.AddDays(1));
            }
            else
            {
                // CARGA POR DEFECTO: Traemos lo de hoy Y lo que esté pendiente/en preparación de antes
                // Esto permite que los botones rápidos funcionen sin recargar la página
                var hoy = DateTime.Today;
                query = query.Where(o => o.OrderDate >= hoy || (o.Status == "Pendiente" || o.Status == "En Preparación"));
            }

            var pedidos = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(pedidos);
        }


        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.Orders
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .FirstOrDefaultAsync(m => m.OrderId == id);

            if (pedido == null) return NotFound();

            // 1. Obtenemos el Email del Claim, igual que en MyOrders
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // 2. Si no es Admin, verificamos que el email del pedido coincida con el del Claim
            if (!User.IsInRole("Admin"))
            {
                // Usamos ToLower() para evitar problemas de mayúsculas/minúsculas
                if (string.IsNullOrEmpty(userEmail) || pedido.Email.ToLower() != userEmail.ToLower())
                {
                    return Forbid();
                }
            }

            return View(pedido);
        }

        

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var pedido = await _context.Orders.FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.Status = status; 
            await _context.SaveChangesAsync(); 

            return RedirectToAction("Details", new { id = id }); 
        }
    }
}

