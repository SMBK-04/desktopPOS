using EnterprisePOS.Data;
using EnterprisePOS.Hubs;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;
using EnterprisePOS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace EnterprisePOS.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;
        private readonly IReceiptService _receiptService;
        private readonly IShiftService _shiftService;
        private readonly IHubContext<KitchenHub> _kitchenHub;
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<POSController> _logger;

        public POSController(
            IProductService productService,
            ISaleService saleService,
            IReceiptService receiptService,
            IShiftService shiftService,
            IHubContext<KitchenHub> kitchenHub,
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<POSController> logger)
        {
            _productService = productService;
            _saleService = saleService;
            _receiptService = receiptService;
            _shiftService = shiftService;
            _kitchenHub = kitchenHub;
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>Main POS screen with products, categories, customers and held orders.</summary>
        public async Task<IActionResult> Index()
        {
            var activeShift = await _shiftService.GetActiveShiftAsync();
            if (activeShift == null)
            {
                TempData["Error"] = "You must Start the Day before accessing the POS screen.";
                return RedirectToAction("Index", "Dashboard");
            }

            var products = (await _productService.GetAllAsync(activeOnly: false))
                .Select(p => new PosProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.SellingPrice,
                    TaxRate = p.TaxRate,
                    Barcode = p.Barcode,
                    ImagePath = p.ImagePath,
                    CategoryName = p.Category.Name,
                    Stock = p.IsActive ? 99999 : 0
                }).ToList();

            var categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
            var customers = await _db.Customers.Where(c => c.IsActive).ToListAsync();
            var heldOrders = await _saleService.GetHeldOrdersAsync();
            var deals = await _db.Deals.Where(d => d.IsActive).ToListAsync();

            var vm = new PosScreenViewModel
            {
                Products = products,
                Categories = categories,
                Customers = customers,
                HeldOrders = heldOrders.ToList()
            };

            ViewBag.Deals = deals;

            var tenantId = _db.CurrentTenantId;
            if (tenantId == Guid.Empty && Guid.TryParse(User.FindFirst("TenantId")?.Value, out var parsedId))
            {
                tenantId = parsedId;
            }

            if (tenantId != Guid.Empty)
            {
                var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
                if (tenant != null)
                {
                    ViewBag.IsSubscriptionExpired = TimeHelper.PKT > tenant.SubscriptionEndDate;
                    ViewBag.SubscriptionEndDate = tenant.SubscriptionEndDate;
                }
            }

            return View(vm);
        }

        /// <summary>Search products by name or barcode (AJAX).</summary>
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<object>());

            var results = await _productService.SearchAsync(term);
            var data = results.Select(p => new
            {
                p.Id, p.Name, price = p.SellingPrice, p.TaxRate, p.Barcode,
                p.ImagePath, currentStock = p.IsActive ? 99999 : 0, category = p.Category.Name
            });
            return Json(data);
        }

        /// <summary>Get product by barcode (AJAX — barcode scanner support).</summary>
        [HttpGet]
        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            var product = await _productService.GetByBarcodeAsync(barcode);
            if (product == null) return NotFound();
            return Json(new
            {
                product.Id, product.Name, price = product.SellingPrice,
                product.TaxRate, product.Barcode, product.ImagePath,
                currentStock = product.IsActive ? 99999 : 0, category = product.Category.Name
            });
        }

        /// <summary>
        /// Processes checkout, generates dual receipts, pushes to Kitchen via SignalR.
        /// Returns both receipt PDFs as a ZIP or JSON with download links.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var cashierId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

                // Process the sale
                var sale = await _saleService.ProcessCheckoutAsync(model, cashierId);

                // Load full sale with navigation properties for receipts
                var fullSale = await _saleService.GetByIdAsync(sale.Id)!;

                // Generate dual receipts
                var cashierPdf = _receiptService.GenerateCashierReceipt(fullSale!);
                var kitchenPdf = _receiptService.GenerateKitchenReceipt(fullSale!);

                // Store receipt bytes temporarily in Cache for download
                _cache.Set($"cashier_{sale.Id}", cashierPdf, TimeSpan.FromMinutes(5));
                _cache.Set($"kitchen_{sale.Id}", kitchenPdf, TimeSpan.FromMinutes(5));

                // Push to Kitchen Display via SignalR
                var kitchenPayload = new
                {
                    invoiceNumber = sale.InvoiceNumber,
                    orderType = sale.OrderType.ToString(),
                    tableNumber = sale.TableNumber,
                    orderToken = sale.OrderToken,
                    items = fullSale!.SaleItems.Select(i => new
                    {
                        name = i.ProductName,
                        quantity = i.Quantity,
                        modifiers = i.Modifiers
                    }),
                    notes = sale.Notes,
                    createdAt = sale.CreatedAt
                };
                await _kitchenHub.Clients.Group("Kitchen").SendAsync("ReceiveOrder", kitchenPayload);

                return Json(new
                {
                    success = true,
                    saleId = sale.Id,
                    invoiceNumber = sale.InvoiceNumber,
                    total = sale.TotalAmount,
                    change = sale.ChangeGiven,
                    cashierReceiptUrl = Url.Action("DownloadReceipt", new { saleId = sale.Id, type = "cashier" }),
                    kitchenReceiptUrl = Url.Action("DownloadReceipt", new { saleId = sale.Id, type = "kitchen" })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout failed.");
                var errorMsg = ex.InnerException != null 
                    ? $"{ex.Message} Inner: {ex.InnerException.Message}" 
                    : ex.Message;
                return StatusCode(500, new { error = errorMsg });
            }
        }

        /// <summary>Downloads cashier or kitchen receipt PDF.</summary>
        public IActionResult DownloadReceipt(int saleId, string type)
        {
            var key = $"{type}_{saleId}";
            if (!_cache.TryGetValue(key, out byte[]? bytes) || bytes == null)
                return NotFound("Receipt not found or expired. Please regenerate from Sales History.");

            return File(bytes, "application/pdf");
        }

        /// <summary>Returns an HTML cashier receipt optimized for 80mm thermal printing.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintCashierReceipt(int saleId)
        {
            var sale = await _saleService.GetByIdAsync(saleId);
            if (sale == null) return NotFound();
            return View("PrintCashierReceipt", sale);
        }

        /// <summary>Returns an HTML kitchen receipt optimized for 80mm thermal printing.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintKitchenReceipt(int saleId)
        {
            var sale = await _saleService.GetByIdAsync(saleId);
            if (sale == null) return NotFound();
            return View("PrintKitchenReceipt", sale);
        }

        /// <summary>Hold current cart, save to DB.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoldOrder([FromBody] HoldOrderViewModel model)
        {
            var cashierId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
            await _saleService.HoldOrderAsync(model, cashierId);
            return Json(new { success = true });
        }

        /// <summary>Resume a held order — returns cart JSON.</summary>
        [HttpGet]
        public async Task<IActionResult> ResumeOrder(int id)
        {
            var held = await _saleService.GetHeldOrderAsync(id);
            if (held == null) return NotFound();

            await _saleService.DeleteHeldOrderAsync(id);
            return Json(new { success = true, cartJson = held.CartJson, table = held.TableNumber });
        }

        /// <summary>Deletes a held order.</summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteHeldOrder(int id)
        {
            var held = await _saleService.GetHeldOrderAsync(id);
            if (held == null) return NotFound();

            await _saleService.DeleteHeldOrderAsync(id);
            return Json(new { success = true });
        }

        /// <summary>Real-time Kitchen Display page.</summary>
        [Authorize(Roles = "Administrator,Manager,KitchenStaff")]
        public IActionResult KitchenDisplay() => View();

        /// <summary>Regenerate receipts for an existing completed sale.</summary>
        public async Task<IActionResult> RegenerateReceipt(int saleId, string type)
        {
            var sale = await _saleService.GetByIdAsync(saleId);
            if (sale == null) return NotFound();

            byte[] pdf = type == "kitchen"
                ? _receiptService.GenerateKitchenReceipt(sale)
                : _receiptService.GenerateCashierReceipt(sale);

            var filename = type == "kitchen"
                ? $"KitchenReceipt_{sale.InvoiceNumber}.pdf"
                : $"CashierReceipt_{sale.InvoiceNumber}.pdf";

            return File(pdf, "application/pdf", filename);
        }
    }
}
