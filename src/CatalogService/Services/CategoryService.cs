using AutoMapper;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Models.DTOs;
using CatalogService.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly CatalogDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;
        private readonly IMessageBus _messageBus;

        public CategoryService(
            CatalogDbContext context, 
            IMapper mapper, 
            ILogger<CategoryService> logger,
            IMessageBus messageBus)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _messageBus = messageBus;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .Include(c => c.SubCategories)
                    .Where(c => c.IsActive)
                    .ToListAsync();

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);
                
                // Set product count
                foreach (var dto in categoryDtos)
                {
                    var category = categories.First(c => c.Id == dto.Id);
                    dto.ProductCount = category.Products?.Count(p => p.IsActive) ?? 0;
                }

                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .Include(c => c.ParentCategory)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (category == null)
                    return null;

                var categoryDto = _mapper.Map<CategoryDto>(category);
                categoryDto.ProductCount = category.Products?.Count(p => p.IsActive) ?? 0;
                categoryDto.ParentCategoryName = category.ParentCategory?.Name;

                return categoryDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID: {CategoryId}", id);
                throw;
            }
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            try
            {
                // Check if parent category exists if provided
                if (createCategoryDto.ParentCategoryId.HasValue)
                {
                    var parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == createCategoryDto.ParentCategoryId && c.IsActive);
                    
                    if (!parentExists)
                    {
                        throw new ArgumentException($"Parent category with ID {createCategoryDto.ParentCategoryId} not found");
                    }
                }

                var category = _mapper.Map<Category>(createCategoryDto);
                category.CreatedAt = DateTime.UtcNow;
                category.IsActive = true;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new category: {CategoryName} with ID: {CategoryId}", category.Name, category.Id);

                // Publish event
                await _messageBus.PublishAsync("category.created", new 
                { 
                    CategoryId = category.Id, 
                    category.Name,
                    category.ParentCategoryId
                });

                return _mapper.Map<CategoryDto>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return null;
                }

                // Check if parent category exists if provided
                if (updateCategoryDto.ParentCategoryId.HasValue)
                {
                    // Prevent circular reference
                    if (updateCategoryDto.ParentCategoryId == id)
                    {
                        throw new ArgumentException("Category cannot be its own parent");
                    }

                    var parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == updateCategoryDto.ParentCategoryId && c.IsActive);
                    
                    if (!parentExists)
                    {
                        throw new ArgumentException($"Parent category with ID {updateCategoryDto.ParentCategoryId} not found");
                    }
                }

                _mapper.Map(updateCategoryDto, category);
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated category: {CategoryId}", id);

                // Publish event
                await _messageBus.PublishAsync("category.updated", new 
                { 
                    CategoryId = id,
                    category.Name,
                    category.ParentCategoryId
                });

                return _mapper.Map<CategoryDto>(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {CategoryId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return false;
                }

                // Check if category has active products
                if (category.Products?.Any(p => p.IsActive) == true)
                {
                    throw new InvalidOperationException("Cannot delete category with active products");
                }

                // Soft delete subcategories
                if (category.SubCategories?.Any() == true)
                {
                    foreach (var subCategory in category.SubCategories)
                    {
                        subCategory.IsActive = false;
                        subCategory.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Soft delete category
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted category: {CategoryId}", id);

                // Publish event
                await _messageBus.PublishAsync("category.deleted", new { CategoryId = id });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(int parentId)
        {
            try
            {
                var subCategories = await _context.Categories
                    .Include(c => c.Products)
                    .Where(c => c.ParentCategoryId == parentId && c.IsActive)
                    .ToListAsync();

                var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(subCategories);
                
                foreach (var dto in categoryDtos)
                {
                    var category = subCategories.First(c => c.Id == dto.Id);
                    dto.ProductCount = category.Products?.Count(p => p.IsActive) ?? 0;
                }

                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subcategories for parent: {ParentId}", parentId);
                throw;
            }
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id && c.IsActive);
        }
    }
}