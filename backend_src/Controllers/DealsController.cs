using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;
using EnterprisePOS.Models.ViewModels;

namespace EnterprisePOS.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class DealsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DealsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var deals = await _db.Deals
                .Include(d => d.DealProducts)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DealListViewModel
                {
                    Id = d.Id,
                    Name = d.Name,
                    Price = d.Price,
                    IsActive = d.IsActive,
                    ProductCount = d.DealProducts.Count
                })
                .ToListAsync();

            return View(deals);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var model = new DealCreateViewModel
            {
                Products = products.Select(p => new DealProductInputModel
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductPrice = p.SellingPrice,
                    Quantity = 1,
                    IsSelected = false
                }).ToList()
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DealCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    var deal = new Deal
                    {
                        Name = model.Name,
                        Price = model.Price,
                        IsActive = model.IsActive,
                        CreatedAt = TimeHelper.PKT
                    };

                    _db.Deals.Add(deal);
                    await _db.SaveChangesAsync();

                    var selectedProducts = model.Products.Where(p => p.IsSelected).ToList();
                    if (selectedProducts.Any())
                    {
                        foreach (var sp in selectedProducts)
                        {
                            _db.DealProducts.Add(new DealProduct
                            {
                                DealId = deal.Id,
                                ProductId = sp.ProductId,
                                Quantity = sp.Quantity
                            });
                        }
                        await _db.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = $"Deal '{model.Name}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Failed to create deal: {ex.Message}");
                    return View(model);
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var deal = await _db.Deals
                .Include(d => d.DealProducts)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deal == null) return NotFound();

            var products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            var linkedProducts = deal.DealProducts.ToDictionary(dp => dp.ProductId, dp => dp.Quantity);

            var model = new DealCreateViewModel
            {
                Name = deal.Name,
                Price = deal.Price,
                IsActive = deal.IsActive,
                Products = products.Select(p => new DealProductInputModel
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductPrice = p.SellingPrice,
                    Quantity = linkedProducts.ContainsKey(p.Id) ? linkedProducts[p.Id] : 1,
                    IsSelected = linkedProducts.ContainsKey(p.Id)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, DealCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var deal = await _db.Deals
                .Include(d => d.DealProducts)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deal == null) return NotFound();

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    deal.Name = model.Name;
                    deal.Price = model.Price;
                    deal.IsActive = model.IsActive;

                    // Remove existing products and add new ones
                    _db.DealProducts.RemoveRange(deal.DealProducts);

                    var selectedProducts = model.Products.Where(p => p.IsSelected).ToList();
                    if (selectedProducts.Any())
                    {
                        foreach (var sp in selectedProducts)
                        {
                            _db.DealProducts.Add(new DealProduct
                            {
                                DealId = deal.Id,
                                ProductId = sp.ProductId,
                                Quantity = sp.Quantity
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Deal '{model.Name}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Failed to update deal: {ex.Message}");
                    return View(model);
                }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id);
            if (deal == null) return NotFound();

            _db.Deals.Remove(deal);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Deal '{deal.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
