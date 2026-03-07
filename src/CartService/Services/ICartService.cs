using CartService.Models;

namespace CartService.Services
{
    public interface ICartService
    {
        Task<Cart?> GetCartAsync(string userId);
        Task<Cart> CreateCartAsync(string userId);
        Task<Cart?> AddItemToCartAsync(string userId, AddToCartRequest request);
        Task<Cart?> UpdateItemQuantityAsync(string userId, string productId, int quantity);
        Task<Cart?> RemoveItemFromCartAsync(string userId, string productId);
        Task<bool> ClearCartAsync(string userId);
        Task<Cart?> ApplyCouponAsync(string userId, string couponCode);
        Task<int> GetCartItemCountAsync(string userId);
    }
}