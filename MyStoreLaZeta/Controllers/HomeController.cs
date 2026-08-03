using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CatalogoPro.Models;
using CatalogoPro.Services;

namespace CatalogoPro.Controllers
{
    public class HomeController(
        CategoryService _categoryService,
        ProductService _productService
        ) : Controller
    {
        public async Task<IActionResult> Index(string search = null)
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            var products = await _productService.GetCatalogAsync(search: search);

            var catalog = new CatalogVM
            {
                Categories = categories,
                Products = products
            };

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