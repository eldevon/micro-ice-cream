using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public int ProductCount { get; set; }
        public string? CategoryType { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CategoryDto> SubCategories { get; set; } = new();
    }

    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? IconUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        public string? CategoryType { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateCategoryDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? IconUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        public string? CategoryType { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CategoryWithProductsDto : CategoryDto
    {
        public List<ProductDto> Products { get; set; } = new();
    }

    public class CategoryTreeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int DisplayOrder { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new();
    }
}