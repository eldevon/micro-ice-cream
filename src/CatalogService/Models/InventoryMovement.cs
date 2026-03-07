using System.ComponentModel.DataAnnotations;

namespace CatalogService.Models
{
    public class InventoryMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string MovementType { get; set; } = string.Empty; // "RESTOCK", "SALE", "ADJUSTMENT", "RETURN"

        [StringLength(100)]
        public string? Reference { get; set; } // Order ID, Adjustment ID, etc.

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime Timestamp { get; set; }

        [StringLength(100)]
        public string? PerformedBy { get; set; }

        // Navigation property
        public virtual Product? Product { get; set; }
    }
}