using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InventoryManagementSystem.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly MongoDbService _mongoDbService;

        public CategoriesController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // ... (Index and Create GET remain the same) ...
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            // Start with a base filter that matches everything
            var filterBuilder = Builders<Category>.Filter;
            var filter = filterBuilder.Empty;

            // 1. Apply Search Filter (Name)
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchRegex = new BsonRegularExpression(searchString, "i");
                filter &= filterBuilder.Regex(c => c.Name, searchRegex);
            }

            // 2. Apply Status Filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Active")
                {
                    filter &= filterBuilder.Eq(c => c.IsActive, true);
                }
                else if (statusFilter == "Inactive")
                {
                    filter &= filterBuilder.Eq(c => c.IsActive, false);
                }
            }
            // Fetch the filtered list
            var products = await _mongoDbService.Categories.Find(filter).ToListAsync();

            // --- Prepare Data for Dropdowns ---
            var categories = await _mongoDbService.Categories.Find(_ => true).ToListAsync();

            // Pass current filter values back to view to maintain state
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            // *** DATA TRIMMING ***
            if (category.Name != null) category.Name = category.Name.Trim();
            if (category.Description != null) category.Description = category.Description.Trim();

            // Feature 2: Check for duplicates
            var existing = await _mongoDbService.Categories.Find(c => c.Name.ToLower() == category.Name.ToLower()).FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                category.IsActive = true;
                // Audit Fields
                category.CreatedBy = User.Identity.Name;
                category.CreatedAt = DateTime.UtcNow;
                category.LastModifiedBy = User.Identity.Name;
                category.LastModifiedAt = DateTime.UtcNow;

                await _mongoDbService.Categories.InsertOneAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var category = await _mongoDbService.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Category categoryFromForm)
        {
            if (id != categoryFromForm.Id) return NotFound();

            // *** DATA TRIMMING ***
            if (categoryFromForm.Name != null) categoryFromForm.Name = categoryFromForm.Name.Trim();
            if (categoryFromForm.Description != null) categoryFromForm.Description = categoryFromForm.Description.Trim();

            var existing = await _mongoDbService.Categories.Find(c => c.Name.ToLower() == categoryFromForm.Name.ToLower() && c.Id != id).FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            var categoryInDb = await _mongoDbService.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (categoryInDb == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Manual Copy
                categoryInDb.Name = categoryFromForm.Name;
                categoryInDb.Description = categoryFromForm.Description;
                categoryInDb.IsActive = categoryFromForm.IsActive;

                // Audit Fields
                categoryInDb.LastModifiedBy = User.Identity.Name;
                categoryInDb.LastModifiedAt = DateTime.UtcNow;

                await _mongoDbService.Categories.ReplaceOneAsync(c => c.Id == id, categoryInDb);
                return RedirectToAction(nameof(Index));
            }
            return View(categoryFromForm);
        }

        // ... (ToggleStatus methods remain the same) ...
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (id == null) return NotFound();
            var category = await _mongoDbService.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("ToggleStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatusConfirmed(string id)
        {
            var categoryInDb = await _mongoDbService.Categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (categoryInDb != null)
            {
                categoryInDb.IsActive = !categoryInDb.IsActive;
                categoryInDb.LastModifiedBy = User.Identity.Name;
                categoryInDb.LastModifiedAt = DateTime.UtcNow;
                await _mongoDbService.Categories.ReplaceOneAsync(c => c.Id == id, categoryInDb);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}