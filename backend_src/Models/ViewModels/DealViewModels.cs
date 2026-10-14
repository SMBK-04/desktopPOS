using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnterprisePOS.Models.ViewModels
{
    public class DealListViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }

    public class DealProductInputModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsSelected { get; set; }
    }

    public class DealCreateViewModel
    {
        [Required, Display(Name = "Deal Name")]
        public string Name { get; set; } = string.Empty;

        [Required, Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public List<DealProductInputModel> Products { get; set; } = new();
    }
}
