using CheckoutService.Models;

namespace CheckoutService.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentDetails paymentDetails);
        Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}