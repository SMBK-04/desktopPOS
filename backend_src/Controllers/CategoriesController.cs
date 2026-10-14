using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterprisePOS.Data;
using EnterprisePOS.Models.Entities;

namespace EnterprisePOS.Controllers
{
    [Authorize(Roles = "Administrator,Manager")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var maxOrder = await _db.Categories.MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
            return View(new Category { DisplayOrder = maxOrder + 1 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid) return View(category);

            category.CreatedAt = TimeHelper.PKT;
            category.IsActive = true;

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (!ModelState.IsValid) return View(model);

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            category.Name = model.Name;
            category.Description = model.Description;
            category.IconClass = model.IconClass;
            category.ColorHex = model.ColorHex;
            category.DisplayOrder = model.DisplayOrder;
            category.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveUp(int id)
        {
            var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
            var index = categories.FindIndex(c => c.Id == id);
            if (index > 0)
            {
                var current = categories[index];
                var prev = categories[index - 1];

                var temp = current.DisplayOrder;
                if (current.DisplayOrder == prev.DisplayOrder)
                {
                    current.DisplayOrder = index - 1;
                    prev.DisplayOrder = index;
                }
                else
                {
                    current.DisplayOrder = prev.DisplayOrder;
                    prev.DisplayOrder = temp;
                }

                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveDown(int id)
        {
            var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
            var index = categories.FindIndex(c => c.Id == id);
            if (index >= 0 && index < categories.Count - 1)
            {
                var current = categories[index];
                var next = categories[index + 1];

                var temp = current.DisplayOrder;
                if (current.DisplayOrder == next.DisplayOrder)
                {
                    current.DisplayOrder = index + 1;
                    next.DisplayOrder = index;
                }
                else
                {
                    current.DisplayOrder = next.DisplayOrder;
                    next.DisplayOrder = temp;
                }

                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            // Check if category has any products
            var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because it has linked products.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
