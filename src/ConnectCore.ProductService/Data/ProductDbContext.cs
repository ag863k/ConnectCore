using Microsoft.EntityFrameworkCore;
using ConnectCore.Shared.Models;

namespace ConnectCore.ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        var products = GenerateSeedProducts();
        modelBuilder.Entity<Product>().HasData(products);
    }

    private static Product[] GenerateSeedProducts()
    {
        return new[]
        {
            new Product
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Laptop Pro 15",
                Description = "High-performance laptop with 15-inch display, 16GB RAM, and 512GB SSD",
                Price = 1299.99m,
                Category = "Electronics",
                StockQuantity = 50,
                SKU = "LAP-PRO-15-001",
                Status = ProductStatus.Available,
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                CreatedBy = "System"
            },
            new Product
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with optical sensor and long battery life",
                Price = 29.99m,
                Category = "Electronics",
                StockQuantity = 200,
                SKU = "MOUSE-WL-001",
                Status = ProductStatus.Available,
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                CreatedBy = "System"
            },
            new Product
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Name = "Mechanical Keyboard",
                Description = "Professional mechanical keyboard with RGB backlight and tactile switches",
                Price = 149.99m,
                Category = "Electronics",
                StockQuantity = 75,
                SKU = "KEYB-MECH-001",
                Status = ProductStatus.Available,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "System"
            },
            new Product
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Name = "4K Monitor",
                Description = "27-inch 4K UHD monitor with IPS panel and USB-C connectivity",
                Price = 399.99m,
                Category = "Electronics",
                StockQuantity = 30,
                SKU = "MON-4K-27-001",
                Status = ProductStatus.Available,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                CreatedBy = "System"
            },
            new Product
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                Name = "Office Chair",
                Description = "Ergonomic office chair with lumbar support and adjustable height",
                Price = 249.99m,
                Category = "Furniture",
                StockQuantity = 25,
                SKU = "CHAIR-OFF-001",
                Status = ProductStatus.Available,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CreatedBy = "System"
            }
        };
    }
}
