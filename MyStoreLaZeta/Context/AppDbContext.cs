using Microsoft.EntityFrameworkCore;
using CatalogoPro.Entities;

namespace CatalogoPro.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariation> ProductVariations { get; set; }
        public DbSet<User> Users { get; set; }

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
                e.Property("CostPrice").HasColumnType("decimal(10,2)");
                e.HasOne(e => e.Category)
                 .WithMany(p => p.Products)
                 .HasForeignKey(e => e.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey("UserId");
                e.Property("UserId").ValueGeneratedOnAdd();
            });
        }
    }
}
