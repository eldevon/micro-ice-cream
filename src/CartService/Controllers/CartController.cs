using CartService.Models;
using CartService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<Cart>> GetCart(string userId)
        {
            try
            {
                var cart = await _cartService.GetCartAsync(userId);
                
                if (cart == null)
                {
                    cart = await _cartService.CreateCartAsync(userId);
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving the cart");
            }
        }

        [HttpPost("{userId}/items")]
        public async Task<ActionResult<Cart>> AddItemToCart(string userId, AddToCartRequest request)
        {
            try
            {
                var cart = await _cartService.AddItemToCartAsync(userId, request);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}", userId);
                return StatusCode(500, "An error occurred while adding item to cart");
            }
        }

        [HttpPut("{userId}/items/{productId}")]
        public async Task<ActionResult<Cart>> UpdateItemQuantity(
            string userId, 
            string productId, 
            [FromBody] int quantity)
        {
            try
            {
                var cart = await _cartService.UpdateItemQuantityAsync(userId, productId, quantity);
                
                if (cart == null)
                {
                    return NotFound("Cart or item not found");
                }

                return Ok(cart);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item quantity for user {UserId}", userId);
                return StatusCode(500, "An error occurred while updating item quantity");
            }
        }

        [HttpDelete("{userId}/items/{productId}")]
        public async Task<ActionResult<Cart>> RemoveItemFromCart(string userId, string productId)
        {
            try
            {
                var cart = await _cartService.RemoveItemFromCartAsync(userId, productId);
                
                if (cart == null)
                {
                    return NotFound("Cart not found");
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for user {UserId}", userId);
                return StatusCode(500, "An error occurred while removing item from cart");
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> ClearCart(string userId)
        {
            try
            {
                var result = await _cartService.ClearCartAsync(userId);
                
                if (!result)
                {
                    return NotFound("Cart not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return StatusCode(500, "An error occurred while clearing the cart");
            }
        }

        [HttpPost("{userId}/coupon")]
        public async Task<ActionResult<Cart>> ApplyCoupon(string userId, ApplyCouponRequest request)
        {
            try
            {
                var cart = await _cartService.ApplyCouponAsync(userId, request.CouponCode);
                
                if (cart == null)
                {
                    return NotFound("Cart not found");
                }

                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon for user {UserId}", userId);
                return StatusCode(500, "An error occurred while applying coupon");
            }
        }

        [HttpGet("{userId}/count")]
        public async Task<ActionResult<int>> GetCartItemCount(string userId)
        {
            try
            {
                var count = await _cartService.GetCartItemCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for user {UserId}", userId);
                return StatusCode(500, "An error occurred while getting cart item count");
            }
        }
    }
}
