using System;

namespace EnterprisePOS.Models.Entities
{
    public class DealProduct
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DealId { get; set; }
        public Deal Deal { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; } = 1;
    }
}
