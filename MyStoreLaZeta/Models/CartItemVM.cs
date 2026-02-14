namespace MyStoreLaZeta.Models
{
    public class CartItemVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; }

        public string ImageName { get; set; }

        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
