using EnterprisePOS.Data;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ApplicationDbContext db, IWebHostEnvironment env, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _db = db;
            _env = env;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId)
        {
            var products = await _productService.GetAllAsync(activeOnly: false);

            if (!string.IsNullOrWhiteSpace(search))
                products = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                               (p.Barcode != null && p.Barcode.Contains(search)));

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId);

            var vm = products.Select(p => new ProductListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                ProductCode = p.ProductCode,
                Barcode = p.Barcode,
                SKU = p.SKU,
                CategoryName = p.Category.Name,
                SellingPrice = p.SellingPrice,
                CostPrice = p.CostPrice,
                CurrentStock = p.CurrentStock,
                LowStockThreshold = p.LowStockThreshold,
                IsActive = p.IsActive,
                ImagePath = p.ImagePath
            });

            ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            return View(vm);
        }

        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create()
        {
            var vm = new ProductCreateViewModel
            {
                Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }

            string? imagePath = null;
            if (model.ImageFile?.Length > 0)
                imagePath = await SaveImageAsync(model.ImageFile);

            await _productService.CreateAsync(model, imagePath);
            TempData["Success"] = "Product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                ProductCode = product.ProductCode,
                Barcode = product.Barcode,
                SKU = product.SKU,
                Description = product.Description,
                ProductType = product.ProductType,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                TaxRate = product.TaxRate,
                CurrentStock = product.CurrentStock,
                LowStockThreshold = product.LowStockThreshold,
                ReorderLevel = product.ReorderLevel,
                CategoryId = product.CategoryId,
                IsActive = product.IsActive,
                TrackInventory = product.TrackInventory,
                ExistingImagePath = product.ImagePath,
                Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }

            string? imagePath = null;
            if (model.ImageFile?.Length > 0)
                imagePath = await SaveImageAsync(model.ImageFile);

            await _productService.UpdateAsync(id, model, imagePath);
            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            await _productService.ToggleActiveAsync(id);
            TempData["Success"] = "Product status updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, HttpGet]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> HardDelete(int id)
        {
            try
            {
                var permanentlyDeleted = await _productService.HardDeleteAsync(id);
                if (permanentlyDeleted)
                {
                    TempData["Success"] = "Product permanently deleted from menu.";
                }
                else
                {
                    TempData["Warning"] = "Product has previous sales records. It has been deactivated and removed from active menus to preserve financial history.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not delete product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Export all products to CSV download.</summary>
        public async Task<IActionResult> ExportCsv()
        {
            var bytes = await _productService.ExportToCsvAsync();
            return File(bytes, "text/csv", $"Products_{DateTime.Today:yyyy-MM-dd}.csv");
        }

        /// <summary>Import products from uploaded CSV file.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid CSV file.";
                return RedirectToAction(nameof(Index));
            }

            using var stream = file.OpenReadStream();
            var count = await _productService.ImportFromCsvAsync(stream);
            TempData["Success"] = $"Successfully imported {count} products.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }
    }
}
