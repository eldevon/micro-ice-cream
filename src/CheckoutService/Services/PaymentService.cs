using CheckoutService.Models;

namespace CheckoutService.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ILogger<PaymentService> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentDetails paymentDetails)
        {
            // In production, this would integrate with a real payment gateway like Stripe, PayPal, etc.
            
            try
            {
                _logger.LogInformation("Processing payment for order {OrderNumber}, amount: {Total}", 
                    order.OrderNumber, order.Total);

                // Simulate payment processing
                await Task.Delay(2000);

                // Simulate success/failure (90% success rate for demo)
                var random = new Random();
                var isSuccess = random.Next(100) < 90;

                if (isSuccess)
                {
                    var transactionId = $"TXN-{Guid.NewGuid():N}".Substring(0, 20).ToUpper();
                    
                    _logger.LogInformation("Payment successful for order {OrderNumber}, transaction: {TransactionId}", 
                        order.OrderNumber, transactionId);

                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = transactionId,
                        ProcessedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.LogWarning("Payment failed for order {OrderNumber}", order.OrderNumber);

                    return new PaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Payment was declined by the bank. Please try another card.",
                        ProcessedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderNumber}", order.OrderNumber);
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing the payment.",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
        {
            try
            {
                _logger.LogInformation("Processing refund for transaction {TransactionId}, amount: {Amount}", 
                    transactionId, amount);

                // Simulate refund processing
                await Task.Delay(1500);

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = $"REF-{transactionId}",
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
                
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing the refund.",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }
    }
}