using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IINSwalayan.Models;

namespace IINSwalayan.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Product configuration
            builder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");
            });

            // Seed Categories
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Makanan", Description = "Berbagai jenis makanan", IconClass = "fas fa-utensils" },
                new Category { Id = 2, Name = "Minuman", Description = "Berbagai jenis minuman", IconClass = "fas fa-coffee" },
                new Category { Id = 3, Name = "Kebutuhan Dapur", Description = "Peralatan dan bahan dapur", IconClass = "fas fa-kitchen-set" },
                new Category { Id = 4, Name = "Skincare", Description = "Produk perawatan kulit", IconClass = "fas fa-spray-can" }
            );

            // Seed sample products
            builder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Indomie Goreng",
                    Description = "Mie instan rasa ayam bawang",
                    Price = 3500,
                    Stock = 100,
                    CategoryId = 1,
                    ImageUrl = "/images/products/indomie.jpg"
                },
                new Product
                {
                    Id = 2,
                    Name = "Aqua 600ml",
                    Description = "Air mineral dalam kemasan",
                    Price = 3000,
                    Stock = 200,
                    CategoryId = 2,
                    ImageUrl = "/images/products/aqua.jpg"
                }
            );
        }
    }
}