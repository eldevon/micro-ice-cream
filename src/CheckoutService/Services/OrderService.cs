using CheckoutService.Data;
using CheckoutService.Models;
using CheckoutService.MessageBus;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CheckoutService.Services
{
    public class OrderService : IOrderService
    {
        private readonly CheckoutDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IMessageBus _messageBus;
        private readonly HttpClient _cartHttpClient;
        private readonly IPaymentService _paymentService;

        public OrderService(
            CheckoutDbContext context,
            ILogger<OrderService> logger,
            IMessageBus messageBus,
            IHttpClientFactory httpClientFactory,
            IPaymentService paymentService)
        {
            _context = context;
            _logger = logger;
            _messageBus = messageBus;
            _paymentService = paymentService;
            _cartHttpClient = httpClientFactory.CreateClient("CartService");
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                return order != null ? MapToResponse(order) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by ID: {OrderId}", id);
                throw;
            }
        }

        public async Task<OrderResponse?> GetOrderByNumberAsync(string orderNumber)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

                return order != null ? MapToResponse(order) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by number: {OrderNumber}", orderNumber);
                throw;
            }
        }

        public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(int userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return orders.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                // Get cart from Cart Service
                var cartResponse = await _cartHttpClient.GetAsync($"api/cart/{request.UserId}");
                
                if (!cartResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Unable to retrieve cart");
                }

                var cartJson = await cartResponse.Content.ReadAsStringAsync();
                var cart = JsonSerializer.Deserialize<Cart>(cartJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cart == null || !cart.Items.Any())
                {
                    throw new InvalidOperationException("Cart is empty");
                }

                // Calculate tax (simplified - 8%)
                var tax = cart.Subtotal * 0.08m;

                // Calculate shipping (simplified)
                var shippingCost = cart.TotalItems * 0.5m; // $0.50 per item

                // Create order
                var order = new Order
                {
                    UserId = request.UserId,
                    UserEmail = request.UserEmail,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    Subtotal = cart.Subtotal,
                    Tax = tax,
                    ShippingCost = shippingCost,
                    Discount = cart.DiscountAmount,
                    CouponCode = cart.CouponCode,
                    ShippingAddress = request.ShippingAddress,
                    ShippingCity = request.ShippingCity,
                    ShippingState = request.ShippingState,
                    ShippingZipCode = request.ShippingZipCode,
                    PaymentMethod = request.PaymentMethod,
                    CreatedAt = DateTime.UtcNow,
                    Items = cart.Items.Select(i => new OrderItem
                    {
                        ProductId = int.Parse(i.ProductId),
                        ProductName = i.ProductName,
                        Flavor = i.Flavor,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        ImageUrl = i.ImageUrl
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created order {OrderNumber} for user {UserId}", 
                    order.OrderNumber, request.UserId);

                // Process payment if payment details provided
                if (request.PaymentDetails != null)
                {
                    var paymentResult = await _paymentService.ProcessPaymentAsync(order, request.PaymentDetails);
                    
                    if (paymentResult.Success)
                    {
                        order.Status = OrderStatus.PaymentCompleted;
                        order.PaymentTransactionId = paymentResult.TransactionId;
                        order.PaymentDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        order.Status = OrderStatus.PaymentFailed;
                        await _context.SaveChangesAsync();
                        throw new InvalidOperationException($"Payment failed: {paymentResult.ErrorMessage}");
                    }
                }

                // Clear cart after successful order
                await _cartHttpClient.DeleteAsync($"api/cart/{request.UserId}");

                // Publish events
                await _messageBus.PublishAsync("order.created", new
                {
                    OrderId = order.Id,
                    order.OrderNumber,
                    order.UserId,
                    order.Total,
                    Items = order.Items.Count
                });

                if (order.Status == OrderStatus.PaymentCompleted)
                {
                    await _messageBus.PublishAsync("payment.completed", new
                    {
                        OrderId = order.Id,
                        order.OrderNumber,
                        order.PaymentTransactionId
                    });
                }

                return MapToResponse(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
                throw;
            }
        }

        public async Task<OrderResponse?> UpdateOrderStatusAsync(int id, OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return null;
                }

                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated order {OrderId} status to {Status}", id, status);

                // Publish event
                await _messageBus.PublishAsync("order.status.updated", new
                {
                    OrderId = id,
                    order.OrderNumber,
                    Status = status.ToString()
                });

                return await GetOrderByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                throw;
            }
        }

        public async Task<OrderResponse?> ProcessPaymentAsync(int id, bool success, string? transactionId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return null;
                }

                if (success)
                {
                    order.Status = OrderStatus.PaymentCompleted;
                    order.PaymentTransactionId = transactionId;
                    order.PaymentDate = DateTime.UtcNow;
                }
                else
                {
                    order.Status = OrderStatus.PaymentFailed;
                }

                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Processed payment for order {OrderId}: Success={Success}", 
                    id, success);

                // Publish event
                if (success)
                {
                    await _messageBus.PublishAsync("payment.completed", new
                    {
                        OrderId = id,
                        order.OrderNumber,
                        TransactionId = transactionId
                    });
                }

                return await GetOrderByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderId}", id);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return false;
                }

                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                {
                    throw new InvalidOperationException("Cannot cancel order that has been shipped or delivered");
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled order {OrderId}", id);

                // Publish event
                await _messageBus.PublishAsync("order.cancelled", new
                {
                    OrderId = id,
                    order.OrderNumber
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.Status == status)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return orders.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by status: {Status}", status);
                throw;
            }
        }

        private OrderResponse MapToResponse(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                ShippingCost = order.ShippingCost,
                Discount = order.Discount,
                Total = order.Total,
                TrackingNumber = order.TrackingNumber,
                Carrier = order.Carrier,
                EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                Items = order.Items.Select(i => new OrderItemResponse
                {
                    ProductName = i.ProductName,
                    Flavor = i.Flavor,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice,
                    ImageUrl = i.ImageUrl
                }).ToList()
            };
        }

        // Cart model for deserialization
        private class Cart
        {
            public List<CartItem> Items { get; set; } = new();
            public decimal Subtotal { get; set; }
            public decimal DiscountAmount { get; set; }
            public string? CouponCode { get; set; }
            public int TotalItems { get; set; }
        }

        private class CartItem
        {
            public string ProductId { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string? Flavor { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public string? ImageUrl { get; set; }
        }
    }
}