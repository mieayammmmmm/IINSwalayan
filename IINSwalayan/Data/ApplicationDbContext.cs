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
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Product configuration
            builder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");
            });

            // Order configuration
            builder.Entity<Order>(entity =>
            {
                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");
            });

            // OrderItem configuration
            builder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalPrice)
                    .HasColumnType("decimal(18,2)");
            });

            // Relationships
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Existing seed data
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Makanan", Description = "Berbagai jenis makanan", IconClass = "fas fa-utensils" },
                new Category { Id = 2, Name = "Minuman", Description = "Berbagai jenis minuman", IconClass = "fas fa-coffee" },
                new Category { Id = 3, Name = "Kebutuhan Dapur", Description = "Peralatan dan bahan dapur", IconClass = "fas fa-kitchen-set" },
                new Category { Id = 4, Name = "Skincare", Description = "Produk perawatan kulit", IconClass = "fas fa-spray-can" }
            );

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