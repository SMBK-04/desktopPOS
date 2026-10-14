using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterprisePOS.Models.Entities
{
    public enum OnlineOrderStatus
    {
        New = 0,
        Confirmed = 1,
        Preparing = 2,
        ReadyForDelivery = 3,
        Dispatched = 4,
        Delivered = 5,
        Rejected = 6
    }

    public enum OnlineOrderType
    {
        Delivery = 0,
        Pickup = 1
    }

    /// <summary>
    /// Represents an order placed from the Breadline E-Commerce website.
    /// </summary>
    public class OnlineOrder : ITenantEntity
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }

        [Required, MaxLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public OnlineOrderStatus Status { get; set; } = OnlineOrderStatus.New;
        public OnlineOrderType OrderType { get; set; } = OnlineOrderType.Delivery;

        // Customer Info
        [Required, MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CustomerEmail { get; set; }

        // Delivery Info
        [MaxLength(500)]
        public string? DeliveryAddress { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public double? CustomerLat { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public double? CustomerLng { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public double? DistanceKm { get; set; }

        // Financials
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Timestamps
        public DateTime PlacedAt { get; set; } = TimeHelper.PKT;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? PreparedAt { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Receipt PDF — generated on confirmation
        public int? LinkedSaleId { get; set; }

        // E-commerce customer reference (external)
        [MaxLength(100)]
        public string? EcomCustomerId { get; set; }

        // Items (stored as JSON)
        [Required]
        public string ItemsJson { get; set; } = "[]";

        // Navigation
        public Sale? LinkedSale { get; set; }
        public ICollection<OnlineOrderReview> Reviews { get; set; } = new List<OnlineOrderReview>();
    }

    /// <summary>
    /// Customer review left after delivery.
    /// </summary>
    public class OnlineOrderReview
    {
        public int Id { get; set; }
        public int OnlineOrderId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = TimeHelper.PKT;

        public OnlineOrder Order { get; set; } = null!;
    }
}
