using CatalogService.Models.DTOs;

namespace CatalogService.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> GetProductBySkuAsync(string sku);
        Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<ProductDto>> GetProductsByFlavorAsync(string flavor);
        Task<IEnumerable<ProductDto>> GetProductsByDietaryRestrictionsAsync(bool? sugarFree, bool? vegan, bool? glutenFree);
        Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int quantity);
        Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<ProductDto>> GetProductsOnSaleAsync();
        
        // Reviews
        Task<ProductReviewDto> AddReviewAsync(CreateProductReviewDto reviewDto);
        Task<IEnumerable<ProductReviewDto>> GetProductReviewsAsync(int productId);
        Task<double> GetAverageRatingAsync(int productId);
    }
}