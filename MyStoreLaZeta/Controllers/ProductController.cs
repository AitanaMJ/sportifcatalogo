using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;

namespace MyStoreLaZeta.Controllers
{
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

            // 1. Limpieza de validaciones (Perfecto)
            ModelState.Remove("Categories");
            ModelState.Remove("Category");
            ModelState.Remove("Category.Name");
            ModelState.Remove("ImageName");

            if (!ModelState.IsValid)
            {
                // 2. Recargar lista si hay error (Perfecto)
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
                if (entityVM.ProductId == 0)
                {
                    // --- LOGICA DE CREAR (Está perfecta) ---
                    await _productService.AddAsync(entityVM);
                    ViewBag.message = "Producto creado correctamente";

                    ModelState.Clear();
                    entityVM = new ProductVM();

                    var listaCategorias = await _categoryService.GetAllCategoriesAsync();
                    entityVM.Categories = listaCategorias.Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.CategoryId.ToString()
                    }).ToList();

                    return View(entityVM);
                }
                else
                {
                    // --- LOGICA DE EDITAR (AQUÍ ESTABA EL CAMBIO) ---
                    await _productService.EditAsync(entityVM);

                    ViewBag.message = "Producto editado correctamente";

                    // 1. Limpiamos el estado del formulario anterior
                    ModelState.Clear();

                    // 2. Creamos un nuevo objeto vacío (esto pone ProductId en 0)
                    entityVM = new ProductVM();

                    // 3. ¡IMPORTANTE! Volvemos a cargar las categorías para el formulario vacío
                    var listaCategorias = await _categoryService.GetAllCategoriesAsync();
                    entityVM.Categories = listaCategorias.Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.CategoryId.ToString()
                    }).ToList();

                    // 4. Retornamos la vista vacía
                    return View(entityVM);
                }
            }
            catch (Exception ex)
            {
                ViewBag.message = "Error: " + ex.Message;
                // Si falló, hay que recargar categorías también para que no se rompa la vista de error
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
