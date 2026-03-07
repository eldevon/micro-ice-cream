using CheckoutService.Models;

namespace CheckoutService.Services
{
    public interface IOrderService
    {
        Task<OrderResponse?> GetOrderByIdAsync(int id);
        Task<OrderResponse?> GetOrderByNumberAsync(string orderNumber);
        Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(int userId);
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
        Task<OrderResponse?> UpdateOrderStatusAsync(int id, OrderStatus status);
        Task<OrderResponse?> ProcessPaymentAsync(int id, bool success, string? transactionId);
        Task<bool> CancelOrderAsync(int id);
        Task<IEnumerable<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status);
    }
}