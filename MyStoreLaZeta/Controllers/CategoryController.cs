
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;

namespace MyStoreLaZeta.Controllers
{
    public class CategoryController(CategoryService _categoryService) : Controller
    {
        public async Task<IActionResult> Index()
        {
         var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> AddEdit(int id)
        {
            if (id == 0)
            {
                return View(new CategoryVM()); 
            }

            var categoryVM = await _categoryService.GetByIdAsync(id);

            if (categoryVM == null)
                return NotFound();

            return View(categoryVM);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(CategoryVM entityVM)
        {
            if (!ModelState.IsValid)
                return View(entityVM);

            if (entityVM.CategoryId == 0)
            {
                await _categoryService.AddAsync(entityVM);
                ViewBag.message = "Categoria creada correctamente";

            }
            else
            {
                await _categoryService.EditAsync(entityVM);
                ViewBag.message = "Categoría editada correctamente";

            }

            return View(entityVM);
        }
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
