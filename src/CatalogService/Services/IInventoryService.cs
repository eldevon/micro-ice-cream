using CatalogService.Models.DTOs;

namespace CatalogService.Services
{
    public interface IInventoryService
    {
        Task<InventoryDto?> GetInventoryByProductIdAsync(int productId);
        Task<IEnumerable<InventoryDto>> GetLowStockInventoriesAsync(int threshold);
        Task<InventoryDto> UpdateStockAsync(int productId, int quantity);
        Task<InventoryDto> AdjustStockAsync(int productId, int adjustment, string reason);
        Task<bool> ValidateStockAsync(int productId, int requestedQuantity);
        Task<IEnumerable<InventoryDto>> GetOutOfStockProductsAsync();
        Task<InventoryDto> SetReorderThresholdAsync(int productId, int threshold);
        Task<InventoryMovementDto> RecordMovementAsync(int productId, int quantity, string movementType, string reference);
    }
}