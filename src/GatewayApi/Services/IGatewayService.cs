namespace GatewayApi.Services
{
    public interface IGatewayService
    {
        // Catalog endpoints
        Task<HttpResponseMessage> ForwardToCatalog(HttpRequest request, string path);
        
        // Cart endpoints
        Task<HttpResponseMessage> ForwardToCart(HttpRequest request, string path);
        
        // Checkout endpoints
        Task<HttpResponseMessage> ForwardToCheckout(HttpRequest request, string path);
        
        // User endpoints
        Task<HttpResponseMessage> ForwardToUser(HttpRequest request, string path);
        
        // Composite endpoints
        Task<HttpResponseMessage> GetUserDashboard(int userId);
        Task<HttpResponseMessage> GetProductDetailsWithReviews(int productId);
    }
}