using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Context;

namespace MyStoreLaZeta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. VISTA USUARIO: Mis Pedidos
        public async Task<IActionResult> MyOrders()
        {
            var userEmail = User.Identity.Name; // Usamos el email porque en tu base de datos vi que UserId está como NULL a veces

            var pedidos = await _context.Orders
                .Include(o => o.OrderItems) // Traemos los items del pedido
                .Where(o => o.Email == userEmail) // Filtramos por el email del usuario logueado
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pedidos);
        }

        // 2. VISTA ADMIN: Gestionar Todos
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageOrders()
        {
            var pedidos = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pedidos);
        }

        // 3. DETALLE DE UN PEDIDO (Para ver qué productos compró)
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (pedido == null) return NotFound();

            // Seguridad: Si no es Admin y el pedido no es suyo, no dejar ver
            if (!User.IsInRole("Admin") && pedido.Email != User.Identity.Name)
            {
                return Forbid();
            }

            return View(pedido);
        }

        // --- AGREGAR ESTO EN OrdersController.cs ---

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var pedido = await _context.Orders.FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.Status = status; // Actualizamos el texto del estado
            await _context.SaveChangesAsync(); // Guardamos en la base de datos

            return RedirectToAction("Details", new { id = id }); // Recargamos la página
        }
    }
}

