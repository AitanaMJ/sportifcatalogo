using Microsoft.EntityFrameworkCore;
using MyStoreLaZeta.Entities;
namespace MyStoreLaZeta.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Category> Category { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; } 
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey("CategoryId");
                e.Property("CategoryId").ValueGeneratedOnAdd();
                e.HasData(
                    new Category { CategoryId = 1, Name = "Textil" },
                    new Category { CategoryId = 2, Name = "Vasos" }
                    );
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey("ProductId");
                e.Property("ProductId").ValueGeneratedOnAdd();
                e.Property("Price").HasColumnType("decimal(10,2)");
                e.HasOne(e => e.Category).WithMany(p => p.Products).HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey("UserId");
                e.Property("UserId").ValueGeneratedOnAdd();
                


            });

            modelBuilder.Entity<Order>(e =>
            {
                e.HasKey("OrderId");
                e.Property("OrderId").ValueGeneratedOnAdd();
                e.Property("TotalAmount").HasColumnType("decimal(10,2)");
                e.HasOne(e => e.User).WithMany(p => p.Orders).HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.HasKey("OrderItemId");
                e.Property("OrderItemId").ValueGeneratedOnAdd();
                e.Property("Price").HasColumnType("decimal(10,2)");
                e.HasOne(e => e.Order).WithMany(p => p.OrderItems).HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);


            });
        }

    }
}
