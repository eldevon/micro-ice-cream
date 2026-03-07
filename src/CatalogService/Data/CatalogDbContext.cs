using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Data
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<InventoryMovement> InventoryMovements { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Inventory> Inventories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // InventoryMovement configuration
            modelBuilder.Entity<InventoryMovement>(entity =>
            {
                entity.HasIndex(m => m.ProductId);
                entity.HasIndex(m => m.MovementType);
                entity.HasIndex(m => m.Timestamp);
                
                entity.HasOne(m => m.Product)
                    .WithMany()
                    .HasForeignKey(m => m.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.SKU).IsUnique();
                entity.HasIndex(p => p.Flavor);
                entity.HasIndex(p => p.CategoryId);

                entity.Property(p => p.Price)
                    .HasPrecision(18, 2);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Name);
                
                entity.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ProductReview configuration
            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
                
                entity.HasOne(r => r.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Inventory configuration
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasIndex(i => i.ProductId).IsUnique();
                
                entity.HasOne(i => i.Product)
                    .WithOne(p => p.Inventory)
                    .HasForeignKey<Inventory>(i => i.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed ice cream categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Classic Flavors", Description = "Timeless ice cream favorites", IconUrl = "/images/categories/classic.png", DisplayOrder = 1, CreatedAt = DateTime.UtcNow, IsActive = true },
                new Category { Id = 2, Name = "Premium", Description = "Luxury ice cream with premium ingredients", IconUrl = "/images/categories/premium.png", DisplayOrder = 2, CreatedAt = DateTime.UtcNow, IsActive = true },
                new Category { Id = 3, Name = "Dairy-Free", Description = "Delicious dairy-free alternatives", IconUrl = "/images/categories/dairy-free.png", DisplayOrder = 3, CreatedAt = DateTime.UtcNow, IsActive = true },
                new Category { Id = 4, Name = "Sugar-Free", Description = "Guilt-free sugar-free options", IconUrl = "/images/categories/sugar-free.png", DisplayOrder = 4, CreatedAt = DateTime.UtcNow, IsActive = true },
                new Category { Id = 5, Name = "Seasonal", Description = "Limited time seasonal flavors", IconUrl = "/images/categories/seasonal.png", DisplayOrder = 5, CreatedAt = DateTime.UtcNow, IsActive = true }
            );

            // Seed ice cream products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Classic Vanilla", Description = "Rich and creamy vanilla ice cream made with Madagascar vanilla beans", Price = 4.99m, CategoryId = 1, Flavor = "Vanilla", IsSugarFree = false, IsVegan = false, IsGlutenFree = true, FatContent = 16, Ingredients = "Milk, Cream, Sugar, Vanilla Extract", Allergens = "Milk", ImageUrl = "/images/products/vanilla.jpg", SKU = "IC-CL-001", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Product { Id = 2, Name = "Belgian Chocolate", Description = "Decadent chocolate ice cream made with premium Belgian cocoa", Price = 5.49m, CategoryId = 1, Flavor = "Chocolate", IsSugarFree = false, IsVegan = false, IsGlutenFree = true, FatContent = 18, Ingredients = "Milk, Cream, Sugar, Cocoa Powder", Allergens = "Milk", ImageUrl = "/images/products/chocolate.jpg", SKU = "IC-CL-002", CreatedAt = DateTime.UtcNow, IsActive = true },
                new Product { Id = 3, Name = "Vegan Coconut Delight", Description = "Creamy coconut-based ice cream, perfect for dairy-free diets", Price = 5.99m, CategoryId = 3, Flavor = "Coconut", IsSugarFree = false, IsVegan = true, IsGlutenFree = true, FatContent = 12, Ingredients = "Coconut Milk, Sugar, Coconut Cream", Allergens = "Tree Nuts (Coconut)", ImageUrl = "/images/products/coconut.jpg", SKU = "IC-DF-001", CreatedAt = DateTime.UtcNow, IsActive = true }
            );

            // Seed inventory
            modelBuilder.Entity<Inventory>().HasData(
                new Inventory { Id = 1, ProductId = 1, QuantityAvailable = 100, ReorderThreshold = 20, MaximumStock = 500, Location = "Aisle 1, Freezer A", LastRestockedAt = DateTime.UtcNow },
                new Inventory { Id = 2, ProductId = 2, QuantityAvailable = 85, ReorderThreshold = 20, MaximumStock = 500, Location = "Aisle 1, Freezer A", LastRestockedAt = DateTime.UtcNow },
                new Inventory { Id = 3, ProductId = 3, QuantityAvailable = 50, ReorderThreshold = 15, MaximumStock = 300, Location = "Aisle 2, Freezer B", LastRestockedAt = DateTime.UtcNow }
            );
        }
    }
}