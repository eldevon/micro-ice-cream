namespace CatalogService.Models.DTOs
{
    public class InventoryDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int QuantityAvailable { get; set; }
        public int? ReorderThreshold { get; set; }
        public int? MaximumStock { get; set; }
        public DateTime? LastRestockedAt { get; set; }
        public string? Location { get; set; }
        public bool IsLowStock => ReorderThreshold.HasValue && QuantityAvailable <= ReorderThreshold.Value;
        public bool IsOutOfStock => QuantityAvailable <= 0;
    }

    public class InventoryMovementDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public string MovementType { get; set; } = string.Empty; // "RESTOCK", "SALE", "ADJUSTMENT", "RETURN"
        public string? Reference { get; set; } // Order ID, Adjustment ID, etc.
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
        public string? PerformedBy { get; set; }
    }

    public class UpdateInventoryDto
    {
        public int QuantityAvailable { get; set; }
        public int? ReorderThreshold { get; set; }
        public int? MaximumStock { get; set; }
        public string? Location { get; set; }
    }

    public class AdjustInventoryDto
    {
        public int Adjustment { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }
}