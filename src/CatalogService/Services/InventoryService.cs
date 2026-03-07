using AutoMapper;
using CatalogService.Data;
using CatalogService.Models;
using CatalogService.Models.DTOs;
using CatalogService.MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly CatalogDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<InventoryService> _logger;
        private readonly IMessageBus _messageBus;

        public InventoryService(
            CatalogDbContext context,
            IMapper mapper,
            ILogger<InventoryService> logger,
            IMessageBus messageBus)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _messageBus = messageBus;
        }

        public async Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
        {
            try
            {
                var inventory = await _context.Inventories
                    .Include(i => i.Product)
                    .FirstOrDefaultAsync(i => i.ProductId == productId);

                return inventory != null ? _mapper.Map<InventoryDto>(inventory) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<InventoryDto>> GetLowStockInventoriesAsync(int threshold)
        {
            try
            {
                var inventories = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.QuantityAvailable <= threshold && i.QuantityAvailable > 0)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<InventoryDto>>(inventories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock inventories with threshold: {Threshold}", threshold);
                throw;
            }
        }

        public async Task<InventoryDto> UpdateStockAsync(int productId, int quantity)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId);

                if (inventory == null)
                {
                    throw new ArgumentException($"Inventory for product {productId} not found");
                }

                var previousQuantity = inventory.QuantityAvailable;
                inventory.QuantityAvailable = quantity;
                inventory.LastRestockedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated stock for product: {ProductId} to {Quantity}", 
                    productId, quantity);

                // Record movement
                await RecordMovementAsync(productId, quantity - previousQuantity, "ADJUSTMENT", "Manual update");

                // Check if reorder is needed
                if (inventory.ReorderThreshold.HasValue && 
                    quantity <= inventory.ReorderThreshold.Value)
                {
                    await _messageBus.PublishAsync("inventory.low", new 
                    { 
                        ProductId = productId, 
                        CurrentStock = quantity,
                        ReorderThreshold = inventory.ReorderThreshold,
                        ProductName = inventory.Product?.Name
                    });
                }

                return _mapper.Map<InventoryDto>(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<InventoryDto> AdjustStockAsync(int productId, int adjustment, string reason)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId);

                if (inventory == null)
                {
                    throw new ArgumentException($"Inventory for product {productId} not found");
                }

                var newQuantity = inventory.QuantityAvailable + adjustment;
                
                if (newQuantity < 0)
                {
                    throw new InvalidOperationException("Insufficient stock for this adjustment");
                }

                inventory.QuantityAvailable = newQuantity;
                if (adjustment > 0)
                {
                    inventory.LastRestockedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Adjusted stock for product: {ProductId} by {Adjustment}. Reason: {Reason}", 
                    productId, adjustment, reason);

                // Record movement
                await RecordMovementAsync(productId, adjustment, adjustment > 0 ? "RESTOCK" : "REMOVE", reason);

                return _mapper.Map<InventoryDto>(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> ValidateStockAsync(int productId, int requestedQuantity)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId);

                if (inventory == null)
                {
                    return false;
                }

                return inventory.QuantityAvailable >= requestedQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stock for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<InventoryDto>> GetOutOfStockProductsAsync()
        {
            try
            {
                var inventories = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.QuantityAvailable <= 0)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<InventoryDto>>(inventories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting out of stock products");
                throw;
            }
        }

        public async Task<InventoryDto> SetReorderThresholdAsync(int productId, int threshold)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId);

                if (inventory == null)
                {
                    throw new ArgumentException($"Inventory for product {productId} not found");
                }

                inventory.ReorderThreshold = threshold;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Set reorder threshold for product: {ProductId} to {Threshold}", 
                    productId, threshold);

                return _mapper.Map<InventoryDto>(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting reorder threshold for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<InventoryMovementDto> RecordMovementAsync(int productId, int quantity, string movementType, string reference)
        {
            try
            {
                // This would typically save to a movements table
                // For now, just log and publish event
                
                var movement = new InventoryMovementDto
                {
                    ProductId = productId,
                    Quantity = quantity,
                    MovementType = movementType,
                    Reference = reference,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Inventory movement recorded: {MovementType} for product {ProductId}, quantity: {Quantity}, reference: {Reference}",
                    movementType, productId, quantity, reference);

                // Publish inventory movement event
                await _messageBus.PublishAsync("inventory.movement", movement);

                return movement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording inventory movement for product: {ProductId}", productId);
                throw;
            }
        }
    }
}