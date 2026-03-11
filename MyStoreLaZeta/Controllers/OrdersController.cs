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

        
        public async Task<IActionResult> MyOrders()
        {
            var userEmail = User.Identity.Name; 

            var pedidos = await _context.Orders
                .Include(o => o.OrderItems) 
                .Where(o => o.Email == userEmail) 
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pedidos);
        }

        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageOrders()
        {
            var pedidos = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pedidos);
        }

       
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (pedido == null) return NotFound();

           
            if (!User.IsInRole("Admin") && pedido.Email != User.Identity.Name)
            {
                return Forbid();
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

