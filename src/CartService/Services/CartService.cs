using CartService.Models;
using CartService.MessageBus;
using StackExchange.Redis;
using System.Text.Json;

namespace CartService.Services
{
    public class CartService : ICartService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<CartService> _logger;
        private readonly IMessageBus _messageBus;
        private readonly HttpClient _httpClient;

        public CartService(
            IConnectionMultiplexer redis,
            ILogger<CartService> logger,
            IMessageBus messageBus,
            IHttpClientFactory httpClientFactory)
        {
            _redis = redis;
            _database = redis.GetDatabase();
            _logger = logger;
            _messageBus = messageBus;
            _httpClient = httpClientFactory.CreateClient();
        }

        private string GetCartKey(string userId) => $"cart:{userId}";

        public async Task<Cart?> GetCartAsync(string userId)
        {
            try
            {
                var cartJson = await _database.StringGetAsync(GetCartKey(userId));
                
                if (cartJson.IsNullOrEmpty)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<Cart>(cartJson!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Cart> CreateCartAsync(string userId)
        {
            try
            {
                var cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                var cartJson = JsonSerializer.Serialize(cart);
                await _database.StringSetAsync(GetCartKey(userId), cartJson, TimeSpan.FromDays(7));

                _logger.LogInformation("Created new cart for user {UserId}", userId);

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Cart?> AddItemToCartAsync(string userId, AddToCartRequest request)
        {
            try
            {
                var cart = await GetCartAsync(userId) ?? await CreateCartAsync(userId);

                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = request.ProductId,
                        ProductName = request.ProductName,
                        UnitPrice = request.UnitPrice,
                        Quantity = request.Quantity,
                        Flavor = request.Flavor,
                        ImageUrl = request.ImageUrl
                    });
                }

                cart.UpdatedAt = DateTime.UtcNow;

                await SaveCartAsync(cart);

                _logger.LogInformation("Added item to cart for user {UserId}", userId);

                // Publish event
                await _messageBus.PublishAsync("cart.item.added", new
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    CartTotal = cart.Total
                });

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Cart?> UpdateItemQuantityAsync(string userId, string productId, int quantity)
        {
            try
            {
                if (quantity < 0)
                {
                    throw new ArgumentException("Quantity cannot be negative");
                }

                var cart = await GetCartAsync(userId);
                if (cart == null)
                {
                    return null;
                }

                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item == null)
                {
                    return null;
                }

                if (quantity == 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                cart.UpdatedAt = DateTime.UtcNow;

                await SaveCartAsync(cart);

                _logger.LogInformation("Updated item quantity in cart for user {UserId}", userId);

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item quantity for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Cart?> RemoveItemFromCartAsync(string userId, string productId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null)
                {
                    return null;
                }

                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item == null)
                {
                    return cart;
                }

                cart.Items.Remove(item);
                cart.UpdatedAt = DateTime.UtcNow;

                await SaveCartAsync(cart);

                _logger.LogInformation("Removed item from cart for user {UserId}", userId);

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null)
                {
                    return false;
                }

                cart.Items.Clear();
                cart.CouponCode = null;
                cart.DiscountAmount = 0;
                cart.UpdatedAt = DateTime.UtcNow;

                await SaveCartAsync(cart);

                _logger.LogInformation("Cleared cart for user {UserId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Cart?> ApplyCouponAsync(string userId, string couponCode)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null)
                {
                    return null;
                }

                // Validate coupon (would call a coupon service)
                var isValid = await ValidateCouponAsync(couponCode, cart.Subtotal);
                
                if (!isValid)
                {
                    throw new InvalidOperationException("Invalid or expired coupon");
                }

                // Calculate discount (simplified - would come from coupon service)
                var discount = await CalculateDiscountAsync(couponCode, cart.Subtotal);

                cart.CouponCode = couponCode;
                cart.DiscountAmount = discount;
                cart.UpdatedAt = DateTime.UtcNow;

                await SaveCartAsync(cart);

                _logger.LogInformation("Applied coupon {CouponCode} to cart for user {UserId}", 
                    couponCode, userId);

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetCartItemCountAsync(string userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                return cart?.TotalItems ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for user {UserId}", userId);
                throw;
            }
        }

        private async Task SaveCartAsync(Cart cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            await _database.StringSetAsync(GetCartKey(cart.UserId), cartJson, TimeSpan.FromDays(7));
        }

        private async Task<bool> ValidateCouponAsync(string couponCode, decimal subtotal)
        {
            // In production, this would call a coupon service
            await Task.Delay(100); // Simulate API call
            
            // Simplified validation
            return couponCode.StartsWith("SAVE") || couponCode.StartsWith("WELCOME");
        }

        private async Task<decimal> CalculateDiscountAsync(string couponCode, decimal subtotal)
        {
            // In production, this would call a coupon service
            await Task.Delay(100); // Simulate API call
            
            // Simplified discount calculation
            if (couponCode == "SAVE10")
                return subtotal * 0.10m;
            if (couponCode == "SAVE20")
                return subtotal * 0.20m;
            if (couponCode == "WELCOME15")
                return subtotal * 0.15m;
            
            return 0;
        }
    }
}