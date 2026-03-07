using System.Text.Json.Serialization;

namespace CartService.Models
{
    public class Cart
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public List<CartItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }

        [JsonIgnore]
        public decimal Subtotal => Items.Sum(i => i.TotalPrice);

        [JsonIgnore]
        public decimal Total => Subtotal - DiscountAmount;

        [JsonIgnore]
        public int TotalItems => Items.Sum(i => i.Quantity);
    }

    public class CartItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Flavor { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Flavor { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ApplyCouponRequest
    {
        public string CouponCode { get; set; } = string.Empty;
    }
}