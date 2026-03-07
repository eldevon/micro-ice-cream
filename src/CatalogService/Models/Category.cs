using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? IconUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        // Navigation properties
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        // For ice cream categories (e.g., "Classic", "Premium", "Seasonal")
        public string? CategoryType { get; set; }
        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
