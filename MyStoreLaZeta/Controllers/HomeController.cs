using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;
using MyStoreLaZeta.Utilities;
using MyStoreLaZeta.Context;

namespace MyStoreLaZeta.Controllers
{
    public class HomeController(
        CategoryService _categoryService,
        ProductService _productService
        
        ) : Controller
    {
        

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var products = await _productService.GetCatalogAsync();
            var catalog = new CatalogVM { Categories = categories, Products = products };
            return View(catalog);
        }

        public async Task<IActionResult>FilterByCategory(int id, string name)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var products = await _productService.GetCatalogAsync(categoryId:id);
            var catalog = new CatalogVM { Categories = categories, Products = products, filterBy=name };
            return View("index", catalog);
        }

        [HttpPost]
        public async Task<IActionResult> FilterBySearch(string value)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var products = await _productService.GetCatalogAsync(search:value);
            var catalog = new CatalogVM { Categories = categories, Products = products, filterBy = $"Resultado para: {value}" };
            return View("index", catalog);
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddItemToCart(int productId, int quantity)
        {
            var product = await _productService.GetByIdAsync(productId);



            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();

            if(cart.Find(x => x.ProductId == productId) == null)
            {
                cart.Add(new CartItemVM
                {
                    ProductId = productId,
                    Name = product.Name,
                    ImageName = product.ImageName,
                    Price = product.Price,
                    Quantity = quantity,
                });
            }
            else
            {
                var updateProduct = cart.Find(x => x.ProductId == productId);
                updateProduct!.Quantity += quantity;
            }

            HttpContext.Session.Set("Cart", cart);
            ViewBag.message = "Producto agregado al carrito";
            return View("ProductDetail",product);
        }


        public IActionResult ViewCart()
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();
            return View(cart);
        }

        public IActionResult RemoveItemToCart(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();

            var product = cart.Find(x => x.ProductId == productId);
            cart.Remove(product!);
            HttpContext.Session.Set("Cart", cart);

            return View("ViewCart",cart);
        }

        // ==========================================
        //  MÉTODOS NUEVOS PARA EL CHECKOUT (CORREGIDOS)
        // ==========================================

        // 1. GET: Muestra la pantalla de Checkout
        public IActionResult Checkout()
        {
            // Recuperamos el carrito de la sesión
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();

            // Si el carrito está vacío, lo mandamos al inicio
            if (cart.Count == 0) return RedirectToAction("Index");

            var model = new CheckoutVM
            {
                CartItems = cart,
                Total = cart.Sum(x => x.Quantity * x.Price),
                ShippingMethod = "Retiro",
                PaymentMethod = "Efectivo"
            };

            return View(model);
        }

        // 2. POST: Procesa la compra (Guarda en BD y manda a WhatsApp)
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(CheckoutVM model, [FromServices] AppDbContext _context)
        {
            // Recuperar carrito otra vez
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart");

            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            // --- A. GUARDAR LA ORDEN EN LA BASE DE DATOS ---
            var order = new Order
            {
                OrderDate = DateTime.Now,
                ClientName = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                // Si elige Retiro, guardamos "Retiro en Local", si no, la dirección que escribió
                Address = model.ShippingMethod == "EnvioDomicilio" ? model.Address : "Retiro en Local",
                ShippingMethod = model.ShippingMethod,
                PaymentMethod = model.PaymentMethod,
                Status = "Pendiente",
                TotalAmount = cart.Sum(x => x.Price * x.Quantity),

                // Mapeamos los items del carrito a la base de datos
                OrderItems = cart.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.Name, // <--- ¡AQUÍ ESTABA EL ERROR! (Corregido a .Name)
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Se genera el ID de la orden

            // --- B. LIMPIAR EL CARRITO ---
            HttpContext.Session.Remove("Cart");

            // --- C. REDIRECCIÓN A WHATSAPP ---

            // ¡IMPORTANTE! CAMBIA ESTO POR TU NÚMERO REAL
            // Formato: 549 + CodArea + Numero (Ej: 5493815555555)
            string miTelefono = "5493810000000";

            // Armamos el mensaje
            // Usamos i.Name aquí también
            string productosTexto = string.Join(", ", cart.Select(x => $"{x.Quantity}x {x.Name}"));

            string mensaje =
                $"Hola LaZeta" +
                $" Hice un nuevo pedido #{order.OrderId}.\n\n" +
                $" Cliente: {order.ClientName}\n" +
                $" Pedido: {productosTexto}\n" +
                $" Total: ${order.TotalAmount:N2}\n" +
                $" Entrega: {order.ShippingMethod}\n" +
                $" Pago: {order.PaymentMethod}\n";

            if (order.ShippingMethod == "EnvioDomicilio")
            {
                mensaje += $" Dirección: {order.Address}\n";
            }

            if (model.PaymentMethod == "MercadoPago")
            {
                mensaje += "\n Espero el link de pago o Alias.";
            }

            // Generamos el link
            string urlWhatsApp = $"https://wa.me/{miTelefono}?text={Uri.EscapeDataString(mensaje)}";

            return Redirect(urlWhatsApp);
        }




        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
