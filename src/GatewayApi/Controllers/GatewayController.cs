using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/gateway")]
    [EnableRateLimiting("fixed")] // Applying default rate limiting to all endpoints
    public class GatewayController : ControllerBase
    {
        private readonly IGatewayService _gatewayService;
        private readonly ILogger<GatewayController> _logger;

        public GatewayController(IGatewayService gatewayService, ILogger<GatewayController> logger)
        {
            _gatewayService = gatewayService;
            _logger = logger;
        }

        #region Catalog Routes

        [HttpGet("catalog/products")]
        public async Task<IActionResult> GetProducts()
        {
            var response = await _gatewayService.ForwardToCatalog(Request, "api/products");
            return await BuildResponse(response);
        }

        [HttpGet("catalog/products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var response = await _gatewayService.ForwardToCatalog(Request, $"api/products/{id}");
            return await BuildResponse(response);
        }

        [HttpGet("catalog/products/{id}/details")]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var response = await _gatewayService.GetProductDetailsWithReviews(id);
            return await BuildResponse(response);
        }

        #endregion

        #region Checkout Routes (Stricter Limits)

        [HttpPost("checkout/orders")]
        [EnableRateLimiting("checkout")] 
        public async Task<IActionResult> CreateOrder()
        {
            var response = await _gatewayService.ForwardToCheckout(Request, "api/orders");
            return await BuildResponse(response);
        }

        #endregion

        #region Health Check (No Rate Limiting)

        [HttpGet("health")]
        [DisableRateLimiting]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        #endregion

        private async Task<IActionResult> BuildResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
    }
}