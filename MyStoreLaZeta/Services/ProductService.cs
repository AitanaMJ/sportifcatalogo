using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;
using Microsoft.IdentityModel.Tokens;


namespace MyStoreLaZeta.Services
{
    public class ProductService(
        GenericRepository<Product> _productRepository,
        GenericRepository<Category> _categoryRepository,
        IWebHostEnvironment _webHostEnvironment
       )
    {
        public async Task<IEnumerable<ProductVM>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync(
            includes: new Expression<Func<Product, object>>[] { x => x.Category! });

            var productsVM = products.Select(item =>
            new ProductVM
            {
                ProductId = item.ProductId,
                Category = new CategoryVM
                {
                    CategoryId = item.Category!.CategoryId,
                    Name = item.Category!.Name,

                },
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Stock = item.Stock,
                ImageName = item.ImageName
            }).ToList();

            return productsVM;
        }


        public async Task<ProductVM> GetByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            var categories = await _categoryRepository.GetAllAsync();

            var productVM = new ProductVM();

            if(product != null)
            {
                productVM = new ProductVM
                {
                    ProductId = product.ProductId,
                    Category = new CategoryVM
                    {
                        CategoryId = product.Category!.CategoryId,
                        Name = product.Category!.Name,
                    },
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageName = product.ImageName,
                };
            }

            productVM.Categories = categories.Select(item => new SelectListItem
            {
                Text = item.Name,
                Value = item.CategoryId.ToString()
            }).ToList();

            return productVM;
        }



        public async Task AddAsync(ProductVM viewModel)
        {
            if (viewModel.ImageFile != null)
            {
                string UploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.ImageFile.FileName);
                string filePath = Path.Combine(UploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                    await viewModel.ImageFile.CopyToAsync(fileStream);

                viewModel.ImageName = uniqueFileName;

            }

            var entity = new Product
            {
                CategoryId = viewModel.Category.CategoryId,
                Name = viewModel.Name,
                Description = viewModel.Description,
                Price = viewModel.Price,
                Stock = viewModel.Stock,
                ImageName = viewModel.ImageName,
            };

            await _productRepository.AddAsync(entity);

        }   

        public async Task EditAsync(ProductVM viewModel)
        {
            var product = await _productRepository.GetByIdAsync(viewModel.ProductId);

            if (viewModel.ImageFile != null) {

                string UploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.ImageFile.FileName);
                string filePath = Path.Combine(UploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                    await viewModel.ImageFile.CopyToAsync(fileStream);

                if (!product.ImageName.IsNullOrEmpty())
                {
                    var previousImage = product.ImageName;
                    string deleteFilePath = Path.Combine(UploadFolder, previousImage);


                    if (File.Exists(deleteFilePath)) File.Delete(deleteFilePath);

                 
                }

                viewModel.ImageName = uniqueFileName;


            }
            else
            {
                viewModel.ImageName = product.ImageName;

            }

            product.CategoryId = viewModel.Category.CategoryId;
            product.Name = viewModel.Name;
            product.Description = viewModel.Description;
            product.Price = viewModel.Price;
            product.Stock = viewModel.Stock;
            product.ImageName = viewModel.ImageName;

            await _productRepository.EditAsync(product);


        }

        public async Task DeleteAsync(int id)
        {
            /*falta eliminar la iamgen*/
            var product = await _productRepository.GetByIdAsync(id);
            await _productRepository.DeleteAsync(product);
        }

        public async Task<IEnumerable<ProductVM>> GetCatalogAsync(int categoryId = 0, string search = "")
        {
            var conditions = new List<Expression<Func<Product, bool>>>
            {
               x => x.Stock > 0
            };

            if (categoryId != 0) conditions.Add(x => x.CategoryId == categoryId);

            // --- CORRECCIÓN AQUÍ ---
            // Agregamos el signo '!' antes de string.IsNullOrEmpty
            if (!string.IsNullOrEmpty(search))
            {
                // Esto busca si el nombre contiene el texto buscado
                conditions.Add(x => x.Name.Contains(search));

                // TIP PRO: Si quisieras buscar también en la descripción, podrías usar esto en su lugar:
                // conditions.Add(x => x.Name.Contains(search) || x.Description.Contains(search));
            }

            var products = await _productRepository.GetAllAsync(
                conditions: conditions.ToArray()
            );

            var productsVM = products.Select(item =>
            new ProductVM
            {
                ProductId = item.ProductId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Stock = item.Stock,
                ImageName = item.ImageName
            }).ToList();

            return productsVM;
        }




    }
}
