using AutoMapper;
using CatalogService.Models;
using CatalogService.Models.DTOs;

namespace CatalogService.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, 
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.StockQuantity,
                    opt => opt.MapFrom(src => src.Inventory != null ? src.Inventory.QuantityAvailable : 0))
                .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewCount, opt => opt.Ignore());

            CreateMap<Product, ProductListDto>()
                .ForMember(dest => dest.StockQuantity,
                    opt => opt.MapFrom(src => src.Inventory != null ? src.Inventory.QuantityAvailable : 0));

            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.Inventory, opt => opt.Ignore());

            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SKU, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.Inventory, opt => opt.Ignore());

            // Category mappings
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentCategoryName, 
                    opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.ProductCount, opt => opt.Ignore())
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore());

            CreateMap<Category, CategoryTreeDto>()
                .ForMember(dest => dest.Children, opt => opt.Ignore())
                .ForMember(dest => dest.ProductCount, 
                    opt => opt.MapFrom(src => src.Products != null ? src.Products.Count(p => p.IsActive) : 0));

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore())
                .ForMember(dest => dest.ParentCategory, opt => opt.Ignore());

            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.SubCategories, opt => opt.Ignore())
                .ForMember(dest => dest.ParentCategory, opt => opt.Ignore());

            // Review mappings
            CreateMap<ProductReview, ProductReviewDto>()
                .ForMember(dest => dest.ProductName, 
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.UserName, opt => opt.Ignore()); // Would come from UserService

            CreateMap<CreateProductReviewDto, ProductReview>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerifiedPurchase, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            // Inventory mappings
            CreateMap<Inventory, InventoryDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null));

            CreateMap<UpdateInventoryDto, Inventory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.LastRestockedAt, opt => opt.Ignore());

            CreateMap<InventoryMovement, InventoryMovementDto>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null));
        }
    }
}