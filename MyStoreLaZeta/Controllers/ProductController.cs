using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;

namespace MyStoreLaZeta.Controllers
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
            ViewBag.message = null;

            
            ModelState.Remove("Categories");
            ModelState.Remove("Category");
            ModelState.Remove("Category.Name");
            ModelState.Remove("ImageName");
            ModelState.Remove("Variations");
            ModelState.Remove("CostPrice");
            ModelState.Remove("Discount");


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

            if (!entityVM.Discount.HasValue)
            {
                entityVM.Discount = 0;
            }

            try
            {
               
                int cantidadRecibida = entityVM.Variations != null ? entityVM.Variations.Count : 0;

                if (entityVM.ProductId == 0)
                {
                    await _productService.AddAsync(entityVM);
                    ViewBag.message = $"¡Éxito! Producto creado. El sistema recibió {cantidadRecibida} variaciones.";

                    ModelState.Clear();
                    entityVM = new ProductVM(); 
                }
                else
                {
                    await _productService.EditAsync(entityVM);
                    ViewBag.message = $"¡Éxito! Producto editado. El sistema recibió {cantidadRecibida} variaciones.";

                    ModelState.Clear();

                    
                    entityVM = await _productService.GetByIdAsync(entityVM.ProductId);
                }

                var listaCategoriasEnd = await _categoryService.GetAllCategoriesAsync();
                entityVM.Categories = listaCategoriasEnd.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.CategoryId.ToString()
                }).ToList();

                return View(entityVM);
            }
            catch (Exception ex)
            {
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ViewBag.message = "Error Base de Datos: " + errorReal;

                var listaCategorias = await _categoryService.GetAllCategoriesAsync();
                entityVM.Categories = listaCategorias.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.CategoryId.ToString()
                }).ToList();

                return View(entityVM);
            }
        }



        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
