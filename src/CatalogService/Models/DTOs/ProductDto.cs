using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Flavor { get; set; } = string.Empty;
        public bool IsSugarFree { get; set; }
        public bool IsVegan { get; set; }
        public bool IsGlutenFree { get; set; }
        public int? FatContent { get; set; }
        public string? Ingredients { get; set; }
        public string? Allergens { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public int StockQuantity { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string? SKU { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateProductDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Flavor { get; set; } = string.Empty;

        public bool IsSugarFree { get; set; }
        public bool IsVegan { get; set; }
        public bool IsGlutenFree { get; set; }
        
        [Range(0, 100)]
        public int? FatContent { get; set; }

        public string? Ingredients { get; set; }
        public string? Allergens { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int InitialStock { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }
    }

    public class UpdateProductDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [StringLength(100)]
        public string? Flavor { get; set; }

        public bool IsSugarFree { get; set; }
        public bool IsVegan { get; set; }
        public bool IsGlutenFree { get; set; }
        
        [Range(0, 100)]
        public int? FatContent { get; set; }

        public string? Ingredients { get; set; }
        public string? Allergens { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageThumbnailUrl { get; set; }
        public string Flavor { get; set; } = string.Empty;
        public bool IsSugarFree { get; set; }
        public bool IsVegan { get; set; }
        public double AverageRating { get; set; }
        public int StockQuantity { get; set; }
    }

    public class ProductReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }

    public class CreateProductReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class ProductSearchDto
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Flavor { get; set; }
        public bool? IsSugarFree { get; set; }
        public bool? IsVegan { get; set; }
        public bool? IsGlutenFree { get; set; }
        public string? SortBy { get; set; } // price, name, rating
        public string? SortOrder { get; set; } // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}