using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _db;

        public InventoryController(IInventoryService inventoryService, ApplicationDbContext db)
        {
            _inventoryService = inventoryService;
            _db = db;
        }

        public async Task<IActionResult> Index(int? productId)
        {
            var logs = await _inventoryService.GetLogsAsync(productId, 100);
            ViewBag.Products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            ViewBag.SelectedProductId = productId;
            return View(logs);
        }

        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Adjust(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            return View(new InventoryAdjustViewModel
            {
                ProductId = id,
                ProductName = product.Name,
                CurrentStock = product.CurrentStock
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Adjust(InventoryAdjustViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
            var movementType = Enum.Parse<InventoryMovementType>(model.MovementType);

            await _inventoryService.AdjustStockAsync(model.ProductId, model.Quantity, movementType, model.Notes ?? "", userId);

            TempData["Success"] = "Stock adjusted successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> LowStock()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.TrackInventory && p.CurrentStock <= p.LowStockThreshold)
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();
            return View(products);
        }
    }
}
