using System.ComponentModel.DataAnnotations;
using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Models.ViewModels
{
    // ─── Cart Item (used in POS JS / JSON) ──────────────────────────────────
    public class CartItemViewModel
    {
        public int? ProductId { get; set; }
        public Guid? DealId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Modifiers { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
    }

    // ─── Checkout (POST from POS screen) ────────────────────────────────────
    public class CheckoutViewModel
    {
        [Required]
        public List<CartItemViewModel> Items { get; set; } = new();

        public int? CustomerId { get; set; }
        public OrderType OrderType { get; set; } = OrderType.Takeaway;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [MaxLength(20)]
        public string? TableNumber { get; set; }

        [MaxLength(50)]
        public string? OrderToken { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ServiceCharge { get; set; }

        public decimal AmountPaid { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    // ─── Hold Order ──────────────────────────────────────────────────────────
    public class HoldOrderViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public string? TableNumber { get; set; }
        public string? CustomerName { get; set; }
    }

    // ─── POS Screen Full ViewModel ───────────────────────────────────────────
    public class PosScreenViewModel
    {
        public List<PosProductViewModel> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<HeldOrder> HeldOrders { get; set; } = new();
    }

    // ─── Sale List/Detail ────────────────────────────────────────────────────
    public class SaleDetailViewModel
    {
        public Sale Sale { get; set; } = null!;
        public bool CanRefund { get; set; }
    }

    public class SaleListViewModel
    {
        public IEnumerable<Sale> Sales { get; set; } = new List<Sale>();
        public DateTime FromDate { get; set; } = DateTime.Today;
        public DateTime ToDate { get; set; } = DateTime.Today;
        public string? StatusFilter { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }
}
