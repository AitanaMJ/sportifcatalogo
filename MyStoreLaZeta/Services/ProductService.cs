using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using MyStoreLaZeta.Context;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Models;
using MyStoreLaZeta.Repositories;



namespace MyStoreLaZeta.Services
{
    public class ProductService(
        GenericRepository<Product> _productRepository,
        GenericRepository<Category> _categoryRepository,
        IWebHostEnvironment _webHostEnvironment,
        AppDbContext _context)

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
                CostPrice = item.CostPrice,
                Discount = item.Discount,
                Price = item.Price,
                Stock = item.Stock,
                ImageName = item.ImageName
            }).ToList();

            return productsVM;
        }


        public async Task<ProductVM> GetByIdAsync(int id)
        {

            var products = await _productRepository.GetAllAsync(
                conditions: new Expression<Func<Product, bool>>[] { x => x.ProductId == id },
                includes: new Expression<Func<Product, object>>[] { x => x.Category! }
            );

            var product = products.FirstOrDefault();
            var categories = await _categoryRepository.GetAllAsync();
            var productVM = new ProductVM();

            if (product != null)
            {

                var variacionesReales = _context.ProductVariations
                                                .Where(v => v.ProductId == product.ProductId)
                                                .ToList();

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
                    CostPrice = product.CostPrice,
                    Discount = product.Discount,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageName = product.ImageName,
                    HasVariations = product.HasVariations,


                    Variations = variacionesReales
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
                {
                    await viewModel.ImageFile.CopyToAsync(fileStream);
                }
                viewModel.ImageName = uniqueFileName;
            }

            var entity = new Product
            {
                CategoryId = viewModel.Category.CategoryId,
                Name = viewModel.Name,
                Description = viewModel.Description,
                CostPrice = viewModel.CostPrice,
                Discount = viewModel.Discount ?? 0,
                Price = viewModel.Price,
                Stock = viewModel.Stock,
                ImageName = viewModel.ImageName,
                HasVariations = viewModel.Variations != null && viewModel.Variations.Any()
            };


            await _productRepository.AddAsync(entity);


            await _context.SaveChangesAsync();


            if (viewModel.Variations != null && viewModel.Variations.Any())
            {
                foreach (var v in viewModel.Variations)
                {
                    v.ProductId = entity.ProductId;
                    _context.ProductVariations.Add(v);
                }


                await _context.SaveChangesAsync();
            }
        }




        public async Task EditAsync(ProductVM viewModel)
        {
            var product = await _productRepository.GetByIdAsync(viewModel.ProductId);
            if (product == null) return;



            if (viewModel.ImageFile != null)
            {

                string UploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.ImageFile.FileName);
                string filePath = Path.Combine(UploadFolder, uniqueFileName);


                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.ImageFile.CopyToAsync(fileStream);
                }


                if (!string.IsNullOrEmpty(product.ImageName))
                {
                    string oldPath = Path.Combine(UploadFolder, product.ImageName);
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }


                product.ImageName = uniqueFileName;
            }

            product.Name = viewModel.Name;
            product.Description = viewModel.Description;
            product.CostPrice = viewModel.CostPrice;
            product.Discount = viewModel.Discount ?? 0;
            product.Price = viewModel.Price;
            product.Stock = viewModel.Stock;
            product.CategoryId = viewModel.Category.CategoryId;
            product.HasVariations = viewModel.Variations != null && viewModel.Variations.Any();


            var oldVariations = _context.ProductVariations.Where(v => v.ProductId == product.ProductId);
            _context.ProductVariations.RemoveRange(oldVariations);


            if (viewModel.Variations != null && viewModel.Variations.Any())
            {
                product.HasVariations = true;
                product.Stock = viewModel.Variations.Sum(v => v.Stock); 

                foreach (var v in viewModel.Variations)
                {
                    v.ProductId = product.ProductId;
                    _context.ProductVariations.Add(v);
                }
            }
            else
            {
                product.HasVariations = false;
                product.Stock = viewModel.Stock; 
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProductVM>> GetCatalogAsync(int categoryId = 0, string search = "")
        {
            var conditions = new List<Expression<Func<Product, bool>>> { x => x.Stock > 0 };

            if (categoryId != 0) conditions.Add(x => x.CategoryId == categoryId);
            if (!string.IsNullOrEmpty(search)) conditions.Add(x => x.Name.Contains(search));


            var products = await _productRepository.GetAllAsync(
                conditions: conditions.ToArray(),
                includes: new Expression<Func<Product, object>>[] { x => x.Category! }
            );

            var productsVM = products.Select(item => new ProductVM
            {
                ProductId = item.ProductId,
                Name = item.Name,
                Description = item.Description,
                CostPrice = item.CostPrice,
                Discount = item.Discount,
                Price = item.Price,
                Stock = item.Stock,
                ImageName = item.ImageName,


                Category = new CategoryVM
                {
                    CategoryId = item.Category!.CategoryId,
                    Name = item.Category!.Name
                }
            }).ToList();

            return productsVM;
        }

    


    public async Task DeleteAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product != null)
            {
                
                if (!string.IsNullOrEmpty(product.ImageName))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", product.ImageName);
                    if (File.Exists(imagePath)) File.Delete(imagePath);
                }

                
                await _productRepository.DeleteAsync(product);
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
