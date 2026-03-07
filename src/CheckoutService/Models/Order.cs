using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckoutService.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OrderNumber { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

        [Required]
        public int UserId { get; set; }

        public string? UserEmail { get; set; }

        public DateTime OrderDate { get; set; }

        public OrderStatus Status { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total => Subtotal + Tax + ShippingCost - Discount;

        public string? CouponCode { get; set; }

        // Shipping Information
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingZipCode { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = "USA";

        // Payment Information
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentTransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }

        // Tracking
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        // Navigation property
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? Flavor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice => UnitPrice * Quantity;

        public string? ImageUrl { get; set; }

        // Navigation property
        public virtual Order? Order { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        PaymentProcessing = 1,
        PaymentCompleted = 2,
        PaymentFailed = 3,
        Processing = 4,
        Shipped = 5,
        Delivered = 6,
        Cancelled = 7,
        Refunded = 8
    }

    public class CreateOrderRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        public string ShippingState { get; set; } = string.Empty;

        [Required]
        public string ShippingZipCode { get; set; } = string.Empty;

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public PaymentDetails? PaymentDetails { get; set; }

        public string? CouponCode { get; set; }

        public string? UserEmail { get; set; }
    }

    public class PaymentDetails
    {
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
    }

    public class OrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
    }

    public class OrderItemResponse
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Flavor { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}