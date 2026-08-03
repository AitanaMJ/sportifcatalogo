namespace CatalogoPro.Models
{
    public class CatalogVM
    {
        public IEnumerable<CategoryVM> Categories { get; set; } = null!;
        public IEnumerable<ProductVM> Products { get; set; } = null!;
        public string filterBy { get; set; } = null!;
    }
}
