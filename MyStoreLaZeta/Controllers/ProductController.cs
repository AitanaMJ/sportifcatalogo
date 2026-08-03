using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CatalogoPro.Models;
using CatalogoPro.Services;

namespace CatalogoPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController(ProductService _productService, CategoryService _categoryService) : Controller
    {
        public async Task<IActionResult> Index()
        {
           
            var products = await _productService.GetAllAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> AddEdit(int id)
        {
            ProductVM productVM;

            if (id == 0)
            {
                productVM = new ProductVM();
            }
            else
            {
                productVM = await _productService.GetByIdAsync(id);
                if (productVM == null) return NotFound();
            }

            var listaCategorias = await _categoryService.GetAllCategoriesAsync();

            productVM.Categories = listaCategorias.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.CategoryId.ToString()
            }).ToList();

            return View(productVM);
        }


        [HttpPost]
        public async Task<IActionResult> AddEdit(ProductVM entityVM)
        {
          
            ModelState.Remove("Categories");
            ModelState.Remove("Category");
            ModelState.Remove("Category.Name");
            ModelState.Remove("ImageName");
            ModelState.Remove("Variations");

            var variationErrors = ModelState.Keys.Where(k => k.StartsWith("Variations")).ToList();
            foreach (var key in variationErrors)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                var listaCategorias = await _categoryService.GetAllCategoriesAsync();
                entityVM.Categories = listaCategorias.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.CategoryId.ToString()
                }).ToList();

                return View(entityVM);
            }

            try
            {
                int cantidadRecibida = entityVM.Variations != null ? entityVM.Variations.Count : 0;

                if (entityVM.ProductId == 0)
                {
                    await _productService.AddAsync(entityVM);
                    TempData["message"] = $"¡Éxito! Producto creado correctamente con {cantidadRecibida} variaciones.";
                }
                else
                {
                    await _productService.EditAsync(entityVM);
                    TempData["message"] = "Producto actualizado correctamente.";
                }

                return RedirectToAction("Index"); 
            }
            catch (Exception ex)
            {
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ViewBag.message = "Error en la operación: " + errorReal;

                var listaCategorias = await _categoryService.GetAllCategoriesAsync();
                entityVM.Categories = listaCategorias.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.CategoryId.ToString()
                }).ToList();

                return View(entityVM);
            }
        }

       
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var productVM = await _productService.GetByIdAsync(id);
            if (productVM != null)
            {
                // El signo '!' invierte el valor: si es true pasa a false, y viceversa
                productVM.IsActive = !productVM.IsActive;

                await _productService.EditAsync(productVM);

                return Json(new { success = true, newState = productVM.IsActive });
                
            }
            return Json(new { success = false });
        }
    }
}
