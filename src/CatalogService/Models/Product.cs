using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatalogService.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        // Ice cream specific properties
        [Required]
        public string Flavor { get; set; } = string.Empty;

        public bool IsSugarFree { get; set; }
        public bool IsVegan { get; set; }
        public bool IsGlutenFree { get; set; }
        
        [Range(0, 100)]
        public int? FatContent { get; set; } // Percentage

        public string? Ingredients { get; set; }
        public string? Allergens { get; set; }

        // Media
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }

        // Navigation properties
        public virtual Category? Category { get; set; }
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public virtual Inventory? Inventory { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        [StringLength(50)]
        public string? SKU { get; set; }
    }

    public class ProductReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }

        public virtual Product? Product { get; set; }
    }

    public class Inventory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int QuantityAvailable { get; set; }

        [Range(0, int.MaxValue)]
        public int? ReorderThreshold { get; set; }

        [Range(0, int.MaxValue)]
        public int? MaximumStock { get; set; }

        public DateTime? LastRestockedAt { get; set; }
        public string? Location { get; set; }

        public virtual Product? Product { get; set; }
    }
}
