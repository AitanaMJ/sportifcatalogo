using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;

namespace MyStoreLaZeta.Services
{
    public class CategoryService(GenericRepository<Category> _categoryRepository)
    {
        public async Task<IEnumerable<CategoryVM>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var categoriesVM = categories.Select(item =>
        new CategoryVM
        {
            CategoryId = item.CategoryId,
            Name = item.Name,
        }


         ).ToList();

            return categoriesVM;
        }

        public async Task AddAsync(CategoryVM viewModel)
        {
            var entity = new Category
            {
                Name = viewModel.Name,
            };
            await _categoryRepository.AddAsync(entity);
        }

        public async Task<CategoryVM?> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            var categoryVM = new CategoryVM();

            if (category != null)
            {
                categoryVM.Name = category.Name;
                categoryVM.CategoryId = category.CategoryId;

            }

            return categoryVM;

        }

        public async Task EditAsync(CategoryVM viewModel)
        {
            var entity = new Category
            {
                Name = viewModel.Name,
                CategoryId = viewModel.CategoryId,
            };
            await _categoryRepository.EditAsync(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category != null)
            {
                await _categoryRepository.DeleteAsync(category);
            }
        }
    }
        
} 
