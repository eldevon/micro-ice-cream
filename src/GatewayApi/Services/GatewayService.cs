using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GatewayApi.Services
{
    public class GatewayService : IGatewayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GatewayService> _logger;

        public GatewayService(
            IHttpClientFactory httpClientFactory,
            ILogger<GatewayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> ForwardToCatalog(HttpRequest request, string path)
        {
            return await ForwardRequest("CatalogService", request, path);
        }

        public async Task<HttpResponseMessage> ForwardToCart(HttpRequest request, string path)
        {
            return await ForwardRequest("CartService", request, path);
        }

        public async Task<HttpResponseMessage> ForwardToCheckout(HttpRequest request, string path)
        {
            return await ForwardRequest("CheckoutService", request, path);
        }

        public async Task<HttpResponseMessage> ForwardToUser(HttpRequest request, string path)
        {
            return await ForwardRequest("UserService", request, path);
        }

        public async Task<HttpResponseMessage> GetUserDashboard(int userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Get user info
                var userResponse = await _httpClientFactory.CreateClient("UserService")
                    .GetAsync($"api/users/{userId}");
                
                if (!userResponse.IsSuccessStatusCode)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();

                // Get recent orders
                var ordersResponse = await _httpClientFactory.CreateClient("CheckoutService")
                    .GetAsync($"api/orders/user/{userId}");
                
                var ordersJson = ordersResponse.IsSuccessStatusCode 
                    ? await ordersResponse.Content.ReadAsStringAsync() 
                    : "[]";

                // Get cart count
                var cartResponse = await _httpClientFactory.CreateClient("CartService")
                    .GetAsync($"api/cart/{userId}/count");
                
                var cartCount = cartResponse.IsSuccessStatusCode 
                    ? await cartResponse.Content.ReadAsStringAsync() 
                    : "0";

                // Combine responses
                var dashboard = new
                {
                    User = JsonSerializer.Deserialize<object>(userJson),
                    RecentOrders = JsonSerializer.Deserialize<object>(ordersJson),
                    CartItemCount = cartCount
                };

                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(
                    JsonSerializer.Serialize(dashboard),
                    Encoding.UTF8,
                    "application/json");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user dashboard for user {UserId}", userId);
                throw;
            }
        }

        public async Task<HttpResponseMessage> GetProductDetailsWithReviews(int productId)
        {
            try
            {
                // Get product details from catalog
                var productResponse = await _httpClientFactory.CreateClient("CatalogService")
                    .GetAsync($"api/products/{productId}");
                
                if (!productResponse.IsSuccessStatusCode)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }

                var productJson = await productResponse.Content.ReadAsStringAsync();

                // Get reviews from catalog
                var reviewsResponse = await _httpClientFactory.CreateClient("CatalogService")
                    .GetAsync($"api/products/{productId}/reviews");
                
                var reviewsJson = reviewsResponse.IsSuccessStatusCode 
                    ? await reviewsResponse.Content.ReadAsStringAsync() 
                    : "[]";

                // Get inventory status
                var inventoryResponse = await _httpClientFactory.CreateClient("CatalogService")
                    .GetAsync($"api/inventory/{productId}");
                
                var inventoryJson = inventoryResponse.IsSuccessStatusCode 
                    ? await inventoryResponse.Content.ReadAsStringAsync() 
                    : "{}";

                // Combine responses
                var productDetails = new
                {
                    Product = JsonSerializer.Deserialize<object>(productJson),
                    Reviews = JsonSerializer.Deserialize<object>(reviewsJson),
                    Inventory = JsonSerializer.Deserialize<object>(inventoryJson)
                };

                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(
                    JsonSerializer.Serialize(productDetails),
                    Encoding.UTF8,
                    "application/json");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product details for product {ProductId}", productId);
                throw;
            }
        }

        private async Task<HttpResponseMessage> ForwardRequest(string serviceName, HttpRequest request, string path)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(serviceName);
                
                // Build the target URL
                var targetUrl = $"{client.BaseAddress}{path}{request.QueryString}";
                
                // Create request message
                var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri(targetUrl)
                };

                // Handle request body
                if (request.Body != null && request.ContentLength > 0)
                {
                    // Read the request body
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    var bodyContent = await reader.ReadToEndAsync();
                    
                    // Reset the stream position for potential subsequent reads
                    request.Body.Position = 0;
                    
                    requestMessage.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");
                }

                
                foreach (var header in request.Headers)
                {
                    var headerValues = header.Value.ToString();
                    if (string.IsNullOrEmpty(headerValues))
                        continue;
                        
                    // Skip headers that might cause issues
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                        continue;
                        
                    if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        continue;
                        
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && requestMessage.Content != null)
                    {
                        requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(headerValues);
                        continue;
                    }
                    
                    if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, headerValues))
                    {
                        requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, headerValues);
                    }
                }

                // Specifically handle Authorization header
                var authHeader = request.Headers.Authorization.ToString();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    try
                    {
                        requestMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
                        _logger.LogDebug("Forwarded Authorization header");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse Authorization header");
                    }
                }

                // Log the request details
                _logger.LogDebug("Forwarding {Method} request to {Url}", request.Method, targetUrl);

                // Send request
                var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                
                // Log response
                _logger.LogInformation("Forwarded {Method} request to {Service}{Path} - Status: {StatusCode}", 
                    request.Method, serviceName, path, response.StatusCode);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding request to {Service}{Path}", serviceName, path);
                
                // Return a proper error response
                var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new 
                    { 
                        error = "An error occurred while forwarding the request",
                        service = serviceName,
                        path = path
                    }), Encoding.UTF8, "application/json")
                };
                
                return errorResponse;
            }
        }
    }
}