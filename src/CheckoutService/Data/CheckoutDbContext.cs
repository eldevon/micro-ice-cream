using CheckoutService.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckoutService.Data
{
    public class CheckoutDbContext : DbContext
    {
        public CheckoutDbContext(DbContextOptions<CheckoutDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(o => o.OrderNumber).IsUnique();
                entity.HasIndex(o => o.UserId);
                entity.HasIndex(o => o.Status);

                entity.Property(o => o.OrderNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasMany(o => o.Items)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasIndex(i => i.ProductId);
                
                entity.Property(i => i.UnitPrice)
                    .HasPrecision(18, 2);
            });
        }
    }
}