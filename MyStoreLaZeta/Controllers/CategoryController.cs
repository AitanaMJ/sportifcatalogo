using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting; // <-- Necesario para la subida de archivos
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Services;
using System.IO; // <-- Necesario para manejar rutas de archivos

namespace MyStoreLaZeta.Controllers
{
    [Authorize(Roles = "Admin")]
    // Agregamos IWebHostEnvironment al constructor
    public class CategoryController(CategoryService _categoryService, IWebHostEnvironment _webHostEnvironment) : Controller
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

            // ========================================================
            // NUEVO: LÓGICA PARA GUARDAR LA IMAGEN FÍSICAMENTE
            // ========================================================
            if (entityVM.ImageFile != null)
            {
                // 1. Generamos un nombre único (ej: 423b-891a-remera.jpg)
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(entityVM.ImageFile.FileName);

                // 2. Buscamos la ruta de wwwroot/images
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 3. Copiamos el archivo a esa carpeta
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await entityVM.ImageFile.CopyToAsync(fileStream);
                }

                // 4. Le asignamos solo el nombre al modelo para guardarlo en la Base de Datos
                entityVM.ImageName = uniqueFileName;
            }
            // ========================================================

            if (entityVM.CategoryId == 0)
            {
                await _categoryService.AddAsync(entityVM);
                TempData["message"] = "Categoría creada correctamente";
            }
            else
            {
                await _categoryService.EditAsync(entityVM);
                TempData["message"] = "Categoría editada correctamente";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var categoryVM = await _categoryService.GetByIdAsync(id);
            if (categoryVM != null)
            {
                categoryVM.IsActive = !categoryVM.IsActive;
                await _categoryService.EditAsync(categoryVM);
                return Json(new { success = true, newState = categoryVM.IsActive });
            }
            return Json(new { success = false });
        }
    }
}