using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;

namespace MyStoreLaZeta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController(CategoryService _categoryService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            // El Admin sigue viendo todas (activas e inactivas)
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
                TempData["message"] = "Categoría creada correctamente"; // Usamos TempData para que el mensaje sobreviva al redirect
            }
            else
            {
                await _categoryService.EditAsync(entityVM);
                TempData["message"] = "Categoría editada correctamente";
            }

            // Después de guardar, es mejor volver al Index para ver la lista actualizada
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var categoryVM = await _categoryService.GetByIdAsync(id);
            if (categoryVM != null)
            {
                // Invierte el estado: si es true pasa a false, si es false pasa a true
                categoryVM.IsActive = !categoryVM.IsActive;

                await _categoryService.EditAsync(categoryVM);


                return Json(new { success = true, newState = categoryVM.IsActive });
              
            }
            return Json(new { success = false });
        }
    }
}