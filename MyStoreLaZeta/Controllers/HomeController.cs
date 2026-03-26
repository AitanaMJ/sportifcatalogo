using System.Diagnostics;
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

        public async Task<IActionResult> Index(string search = null)
        {
            // 1. Traemos las categorías (esto queda igual)
            var categories = await _categoryService.GetActiveCategoriesAsync();

            // 2. Traemos los productos, pero ahora le pasamos la palabra buscada
            // Si 'search' está vacío, tu servicio trae todos automáticamente.
            var products = await _productService.GetCatalogAsync(search: search);

            // 3. Armamos el modelo para la vista
            var catalog = new CatalogVM
            {
                Categories = categories,
                Products = products
            };

            // 4. Guardamos la palabra buscada para que la barra de búsqueda no quede en blanco al recargar
            ViewBag.BusquedaActual = search;

            return View(catalog);
        }

        public async Task<IActionResult> Catalogo(string search = null, int? categoryId = null, string categoryName = null, int page = 1)
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
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

           
            //  Validación de Stock antes de agregar
           
            int currentQuantityInCart = existingItem != null ? existingItem.Quantity : 0;
            int requestedTotalQuantity = currentQuantityInCart + quantity;
            int availableStock = 0;

            if (variationId.HasValue)
            {
                var v = product.Variations.FirstOrDefault(x => x.Id == variationId);
                if (v != null) availableStock = v.Stock;
            }
            else
            {
                availableStock = product.Stock;
            }

            if (requestedTotalQuantity > availableStock)
            {
                // Si pide más de lo que hay, no lo dejamos agregar
                ViewBag.errorMessage = $"No hay suficiente stock. (Stock disponible: {availableStock})";
                return View("ProductDetail", product);
            }
            

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
            ViewBag.message = "¡Producto añadido con éxito!";

            return View("ProductDetail", product);
        }

        public IActionResult ViewCart()
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();
            return View(cart);
        }

        public IActionResult RemoveItemToCart(int productId, int? variationId = null)
        {
            var cart = HttpContext.Session.Get<List<CartItemVM>>("Cart") ?? new List<CartItemVM>();
            var item = cart.Find(x => x.ProductId == productId && x.VariationId == variationId);
            if (item != null) cart.Remove(item);

            HttpContext.Session.Set("Cart", cart);
            return RedirectToAction("ViewCart");
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

            // 1. DESCUENTO DE STOCK DIRECTO A SQL (Sin tracking de memoria)
            foreach (var item in cart)
            {
                if (item.VariationId.HasValue && item.VariationId > 0)
                {
                    // Resta a la variación (ropa, etc.)
                    await _context.ProductVariations
                        .Where(v => v.Id == item.VariationId)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock - item.Quantity));

                    // Resta al producto principal
                    await _context.Products
                        .Where(p => p.ProductId == item.ProductId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));
                }
                else
                {
                    // Resta al producto simple (como tu Taza)
                    await _context.Products
                        .Where(p => p.ProductId == item.ProductId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));
                }
            }

            // 2. CREAMOS Y GUARDAMOS LA ORDEN NORMALMENTE
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
                    ProductName = !string.IsNullOrEmpty(i.SizeName) ? $"{i.Name} ({i.ColorName} - {i.SizeName})" : i.Name,
                    Price = i.FinalPrice,
                    Quantity = i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            return RedirectToAction("OrderSuccess", new { id = order.OrderId });
        }

        public async Task<IActionResult> OrderSuccess(int id)
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