using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;
using System.Linq.Expressions; 

namespace MyStoreLaZeta.Services
{
    public class CategoryService(GenericRepository<Category> _categoryRepository)
    {
        public async Task<IEnumerable<CategoryVM>> GetAllCategoriesAsync()
        {
           
            var categories = await _categoryRepository.GetAllAsync();
            var categoriesVM = categories.Select(item => new CategoryVM
            {
                CategoryId = item.CategoryId,
                Name = item.Name,
                IsActive = item.IsActive, // 

                // Mapeo de descripción e imagen
                Description = item.Description,
                ImageName = item.ImageName
            }).ToList();

            return categoriesVM;
        }

        // Método extra para el Cliente (solo activas)
        public async Task<IEnumerable<CategoryVM>> GetActiveCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync(
                conditions: new Expression<Func<Category, bool>>[] { c => c.IsActive }
            );
            return categories.Select(item => new CategoryVM
            {
                CategoryId = item.CategoryId,
                Name = item.Name,
                IsActive = item.IsActive,

               
                Description = item.Description,
                ImageName = item.ImageName
            }).ToList();
        }

        public async Task AddAsync(CategoryVM viewModel)
        {
            var entity = new Category
            {
                Name = viewModel.Name,
                IsActive = true, // Por defecto activa al crear

               
                Description = viewModel.Description,
                ImageName = viewModel.ImageName
            };
            await _categoryRepository.AddAsync(entity);
        }

        public async Task<CategoryVM?> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return null;

            return new CategoryVM
            {
                Name = category.Name,
                CategoryId = category.CategoryId,
                IsActive = category.IsActive, 

               
                Description = category.Description,
                ImageName = category.ImageName
            };
        }

        public async Task EditAsync(CategoryVM viewModel)
        {
           
            var entity = await _categoryRepository.GetByIdAsync(viewModel.CategoryId);
            if (entity != null)
            {
                entity.Name = viewModel.Name;
                entity.IsActive = viewModel.IsActive; 

              
                entity.Description = viewModel.Description;
                entity.ImageName = viewModel.ImageName;

                await _categoryRepository.EditAsync(entity);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category != null)
            {
                category.IsActive = false; 
                await _categoryRepository.EditAsync(category); 
            }
        }
    }
}