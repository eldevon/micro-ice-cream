using AutoMapper;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Models.DTOs;
using CatalogService.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly CatalogDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        private readonly IMessageBus _messageBus;

        public ProductService(
            CatalogDbContext context, 
            IMapper mapper, 
            ILogger<ProductService> logger,
            IMessageBus messageBus)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _messageBus = messageBus;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
                
                // Calculate average ratings
                foreach (var dto in productDtos)
                {
                    var product = products.First(p => p.Id == dto.Id);
                    dto.AverageRating = product.Reviews?.Any() == true 
                        ? product.Reviews.Average(r => r.Rating) 
                        : 0;
                    dto.ReviewCount = product.Reviews?.Count ?? 0;
                    dto.StockQuantity = product.Inventory?.QuantityAvailable ?? 0;
                }

                return productDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                throw;
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                    return null;

                var productDto = _mapper.Map<ProductDto>(product);
                productDto.AverageRating = product.Reviews?.Any() == true 
                    ? product.Reviews.Average(r => r.Rating) 
                    : 0;
                productDto.ReviewCount = product.Reviews?.Count ?? 0;
                productDto.StockQuantity = product.Inventory?.QuantityAvailable ?? 0;

                return productDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<ProductDto?> GetProductBySkuAsync(string sku)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive);

                if (product == null)
                    return null;

                var productDto = _mapper.Map<ProductDto>(product);
                productDto.AverageRating = product.Reviews?.Any() == true 
                    ? product.Reviews.Average(r => r.Rating) 
                    : 0;
                productDto.ReviewCount = product.Reviews?.Count ?? 0;
                productDto.StockQuantity = product.Inventory?.QuantityAvailable ?? 0;

                return productDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by SKU: {SKU}", sku);
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .Where(p => p.CategoryId == categoryId && p.IsActive)
                    .ToListAsync();

                return MapProductsWithDetails(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByFlavorAsync(string flavor)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .Where(p => p.Flavor.ToLower().Contains(flavor.ToLower()) && p.IsActive)
                    .ToListAsync();

                return MapProductsWithDetails(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by flavor: {Flavor}", flavor);
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByDietaryRestrictionsAsync(
            bool? sugarFree, bool? vegan, bool? glutenFree)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive);

                if (sugarFree.HasValue)
                    query = query.Where(p => p.IsSugarFree == sugarFree.Value);

                if (vegan.HasValue)
                    query = query.Where(p => p.IsVegan == vegan.Value);

                if (glutenFree.HasValue)
                    query = query.Where(p => p.IsGlutenFree == glutenFree.Value);

                var products = await query.ToListAsync();
                return MapProductsWithDetails(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by dietary restrictions");
                throw;
            }
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            try
            {
                // Check if category exists
                var category = await _context.Categories.FindAsync(createProductDto.CategoryId);
                if (category == null)
                {
                    throw new ArgumentException($"Category with ID {createProductDto.CategoryId} not found");
                }

                // Check if SKU is unique
                if (!string.IsNullOrEmpty(createProductDto.SKU))
                {
                    var existingProduct = await _context.Products
                        .FirstOrDefaultAsync(p => p.SKU == createProductDto.SKU);
                    
                    if (existingProduct != null)
                    {
                        throw new ArgumentException($"Product with SKU {createProductDto.SKU} already exists");
                    }
                }

                var product = _mapper.Map<Product>(createProductDto);
                product.CreatedAt = DateTime.UtcNow;
                product.IsActive = true;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Create inventory record
                var inventory = new Inventory
                {
                    ProductId = product.Id,
                    QuantityAvailable = createProductDto.InitialStock,
                    LastRestockedAt = DateTime.UtcNow
                };

                _context.Inventories.Add(inventory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new product: {ProductName} with ID: {ProductId}", 
                    product.Name, product.Id);

                // Publish event
                await _messageBus.PublishAsync("product.created", new 
                { 
                    ProductId = product.Id, 
                    product.Name, 
                    product.Price,
                    product.SKU 
                });

                return await GetProductByIdAsync(product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return null;
                }

                // Check if category exists
                var category = await _context.Categories.FindAsync(updateProductDto.CategoryId);
                if (category == null)
                {
                    throw new ArgumentException($"Category with ID {updateProductDto.CategoryId} not found");
                }

                _mapper.Map(updateProductDto, product);
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated product: {ProductId}", id);

                // Publish event
                await _messageBus.PublishAsync("product.updated", new 
                { 
                    ProductId = id, 
                    product.Name, 
                    product.Price 
                });

                return await GetProductByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return false;
                }

                // Soft delete
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted product: {ProductId}", id);

                // Publish event
                await _messageBus.PublishAsync("product.deleted", new { ProductId = id });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId);

                if (inventory == null)
                {
                    return false;
                }

                inventory.QuantityAvailable = quantity;
                inventory.LastRestockedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated stock for product: {ProductId} to {Quantity}", 
                    productId, quantity);

                // Check if reorder is needed
                if (inventory.ReorderThreshold.HasValue && 
                    quantity <= inventory.ReorderThreshold.Value)
                {
                    await _messageBus.PublishAsync("inventory.low", new 
                    { 
                        ProductId = productId, 
                        CurrentStock = quantity,
                        ReorderThreshold = inventory.ReorderThreshold
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllProductsAsync();
                }

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && 
                               (p.Name.Contains(searchTerm) || 
                                p.Description.Contains(searchTerm) ||
                                p.Flavor.Contains(searchTerm) ||
                                p.SKU.Contains(searchTerm)))
                    .ToListAsync();

                return MapProductsWithDetails(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetProductsOnSaleAsync()
        {
            // This would typically check a sales table or promotion service
            // For now, it's returning a subset
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Inventory)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive && p.Id % 2 == 0) // Example condition
                    .Take(5)
                    .ToListAsync();

                return MapProductsWithDetails(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products on sale");
                throw;
            }
        }

        public async Task<ProductReviewDto> AddReviewAsync(CreateProductReviewDto reviewDto)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(reviewDto.ProductId);
                if (product == null)
                {
                    throw new ArgumentException($"Product with ID {reviewDto.ProductId} not found");
                }

                // Check if user already reviewed this product
                var existingReview = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ProductId == reviewDto.ProductId && 
                                             r.UserId == reviewDto.UserId);

                if (existingReview != null)
                {
                    throw new InvalidOperationException("User has already reviewed this product");
                }

                var review = _mapper.Map<ProductReview>(reviewDto);
                review.CreatedAt = DateTime.UtcNow;
                review.IsVerifiedPurchase = false; // Would check orders service

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added review for product: {ProductId}", reviewDto.ProductId);

                return _mapper.Map<ProductReviewDto>(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review");
                throw;
            }
        }

        public async Task<IEnumerable<ProductReviewDto>> GetProductReviewsAsync(int productId)
        {
            try
            {
                var reviews = await _context.ProductReviews
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<ProductReviewDto>>(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            try
            {
                var reviews = await _context.ProductReviews
                    .Where(r => r.ProductId == productId)
                    .ToListAsync();

                return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating for product: {ProductId}", productId);
                throw;
            }
        }

        private IEnumerable<ProductDto> MapProductsWithDetails(List<Product> products)
        {
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            
            foreach (var dto in productDtos)
            {
                var product = products.First(p => p.Id == dto.Id);
                dto.AverageRating = product.Reviews?.Any() == true 
                    ? product.Reviews.Average(r => r.Rating) 
                    : 0;
                dto.ReviewCount = product.Reviews?.Count ?? 0;
                dto.StockQuantity = product.Inventory?.QuantityAvailable ?? 0;
            }

            return productDtos;
        }
    }
}