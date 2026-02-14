using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;
using MyStoreLaZeta.Utilities;

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
