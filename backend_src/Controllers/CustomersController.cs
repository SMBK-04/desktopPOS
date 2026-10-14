using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomersController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search) || (c.Email != null && c.Email.Contains(search)) || (c.Phone != null && c.Phone.Contains(search)));

            var customers = await query.Include(c => c.Sales).ToListAsync();

            var vm = customers.Select(c => new CustomerViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                IsActive = c.IsActive,
                LoyaltyPoints = c.LoyaltyPoints,
                TotalOrders = c.Sales.Count(s => s.Status == SaleStatus.Completed),
                TotalSpent = c.Sales.Where(s => s.Status == SaleStatus.Completed).Sum(s => s.TotalAmount)
            });

            ViewBag.Search = search;
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new CustomerViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var count = await _db.Customers.CountAsync() + 1;
            _db.Customers.Add(new Customer
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                IsActive = model.IsActive,
                CustomerCode = $"CUST-{count:D5}"
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer added.";
            return RedirectToAction(nameof(Index));
        }
    }
}
