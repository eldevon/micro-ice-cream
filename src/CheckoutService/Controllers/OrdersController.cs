using CheckoutService.Models;
using CheckoutService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CheckoutService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponse>> GetOrder(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        [HttpGet("number/{orderNumber}")]
        public async Task<ActionResult<OrderResponse>> GetOrderByNumber(string orderNumber)
        {
            try
            {
                var order = await _orderService.GetOrderByNumberAsync(orderNumber);
                
                if (order == null)
                {
                    return NotFound($"Order with number {orderNumber} not found");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order by number {OrderNumber}", orderNumber);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetUserOrders(int userId)
        {
            try
            {
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var order = await _orderService.CreateOrderAsync(request);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, "An error occurred while creating the order");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
        {
            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(id, status);
                
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating order status");
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(id);
                
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, "An error occurred while cancelling the order");
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }
    }
}