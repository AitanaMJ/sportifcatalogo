using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;
using Microsoft.EntityFrameworkCore;
using MyStoreLaZeta.Utilities;
using MyStoreLaZeta.Context;

namespace MyStoreLaZeta.Controllers
{
    public class HomeController(
        CategoryService _categoryService,
        ProductService _productService,
        AppDbContext _context
        ) : Controller
    {

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var products = await _productService.GetCatalogAsync();
            var catalog = new CatalogVM { Categories = categories, Products = products };
            return View(catalog);
        }

        public async Task<IActionResult> Catalogo(string search = null, int? categoryId = null, string categoryName = null, int page = 1)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            IEnumerable<ProductVM> products;
            string filtroActivo = null;

            if (categoryId.HasValue)
            {
                products = await _productService.GetCatalogAsync(categoryId: categoryId.Value);
                filtroActivo = !string.IsNullOrEmpty(categoryName) ? $"Viendo: {categoryName}" : "Categoría seleccionada";
            }
            else if (!string.IsNullOrEmpty(search))
            {
                products = await _productService.GetCatalogAsync(search: search);
                filtroActivo = $"Resultados para: {search}";
            }
            else
            {
                products = await _productService.GetCatalogAsync();
            }

            int cantidadPorPagina = 6;
            int totalProductos = products.Count();
            int totalPaginas = (int)Math.Ceiling((double)totalProductos / cantidadPorPagina);

            var productosPaginados = products
                .Skip((page - 1) * cantidadPorPagina)
                .Take(cantidadPorPagina)
                .ToList();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.CategoryName = categoryName;

            var model = new CatalogVM
            {
                Categories = categories,
                Products = productosPaginados,
                filterBy = filtroActivo
            };

            return View(model);
        }

        public async Task<IActionResult> FilterByCategory(int id, string name)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var products = await _productService.GetCatalogAsync(categoryId: id);
            var catalog = new CatalogVM { Categories = categories, Products = products, filterBy = name };
            return View("index", catalog);
        }

        [HttpPost]
        public async Task<IActionResult> FilterBySearch(string value)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var products = await _productService.GetCatalogAsync(search: value);
            var catalog = new CatalogVM { Categories = categories, Products = products, filterBy = $"Resultado para: {value}" };
            return View("index", catalog);
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddItemToCart(int productId, int quantity, int? variationId = null)
        {
            var product = await _productService.GetByIdAsync(productId);
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();

            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId && x.VariationId == variationId);

            if (existingItem == null)
            {
                string color = "";
                string size = "";

                if (variationId.HasValue)
                {
                    var v = product.Variations.FirstOrDefault(x => x.Id == variationId);
                    if (v != null)
                    {
                        color = v.Color;
                        size = v.Size;
                    }
                }

                cart.Add(new CartItemVM
                {
                    ProductId = productId,
                    VariationId = variationId,
                    Name = product.Name,
                    ImageName = product.ImageName,
                    Price = product.Price,
                    Discount = product.Discount ?? 0,
                    Quantity = quantity,
                    ColorName = color,
                    SizeName = size
                });
            }
            else
            {
                existingItem.Quantity += quantity;
            }

            HttpContext.Session.Set("Cart", cart);
            ViewBag.message = "¡Producto añadido con éxito!"; // Mensaje corregido

            return View("ProductDetail", product);
        }

        public IActionResult ViewCart()
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();
            return View(cart);
        }

        public IActionResult RemoveItemToCart(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();
            var item = cart.Find(x => x.ProductId == productId);
            if (item != null) cart.Remove(item);

            HttpContext.Session.Set("Cart", cart);
            return View("ViewCart", cart);
        }

        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();
            if (cart.Count == 0) return RedirectToAction("Index");

            var model = new CheckoutVM
            {
                CartItems = cart,
                Total = cart.Sum(x => x.Quantity * x.FinalPrice),
                ShippingMethod = "Retiro",
                PaymentMethod = "Efectivo"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(CheckoutVM model)
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart");
            if (cart == null || cart.Count == 0) return RedirectToAction("Index");

            // 1. DESCUENTO DE STOCK OBLIGATORIO
            foreach (var item in cart)
            {
                if (item.VariationId.HasValue && item.VariationId > 0)
                {
                    // Caso Ropa (Variaciones)
                    var v = await _context.ProductVariations.FindAsync(item.VariationId);
                    if (v != null)
                    {
                        v.Stock -= item.Quantity;
                        // Le decimos a EF: "Oye, esto cambió sí o sí, actualizalo"
                        _context.Entry(v).Property(x => x.Stock).IsModified = true;

                        // Sincronizamos el stock total del producto principal
                        var p = await _context.Products.FindAsync(item.ProductId);
                        if (p != null)
                        {
                            p.Stock -= item.Quantity;
                            _context.Entry(p).Property(x => x.Stock).IsModified = true;
                        }
                    }
                }
                else
                {
                    // Caso Taza (Producto Simple)
                    var p = await _context.Products.FindAsync(item.ProductId);
                    if (p != null)
                    {
                        p.Stock -= item.Quantity;
                        // Forzamos la marca de modificación en la columna Stock
                        _context.Entry(p).Property(x => x.Stock).IsModified = true;
                    }
                }
            }

            // 2. CREACIÓN DE LA ORDEN (Esto ya sabemos que te funciona bien)
            var order = new Order
            {
                OrderDate = DateTime.Now,
                ClientName = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.ShippingMethod == "EnvioDomicilio" ? model.Address : "Retiro en Local",
                ShippingMethod = model.ShippingMethod,
                PaymentMethod = model.PaymentMethod,
                Status = "Pendiente",
                TotalAmount = cart.Sum(x => x.FinalPrice * x.Quantity),
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

            // 3. GUARDADO FINAL
            // Aquí EF enviará los INSERT de la orden y los UPDATE del stock
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            return RedirectToAction("OrderSuccess", new { id = order.OrderId });
        }

        public async Task<IActionResult> OrderSuccess(int id, [FromServices] AppDbContext _context)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
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