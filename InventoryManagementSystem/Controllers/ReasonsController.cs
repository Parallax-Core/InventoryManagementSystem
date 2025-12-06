using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace InventoryManagementSystem.Controllers
{
    [Authorize] // Ensure only logged-in users can access this
    public class ReasonsController : Controller
    {
        private readonly MongoDbService _mongoDbService;

        public ReasonsController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // GET: Reasons
        public async Task<IActionResult> Index()
        {
            var reasons = await _mongoDbService.Reasons.Find(_ => true).ToListAsync();
            return View(reasons);
        }

        // GET: Reasons/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reasons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Type")] Reason reason)
        {
            // Data Trimming
            if (reason.Name != null) reason.Name = reason.Name.Trim();
            if (reason.Description != null) reason.Description = reason.Description.Trim();

            // Duplicate Check
            var existing = await _mongoDbService.Reasons.Find(r => r.Name.ToLower() == reason.Name.ToLower()).FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("Name", "This reason already exists.");
            }

            if (ModelState.IsValid)
            {
                await _mongoDbService.Reasons.InsertOneAsync(reason);
                return RedirectToAction(nameof(Index));
            }
            return View(reason);
        }

        // GET: Reasons/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var reason = await _mongoDbService.Reasons.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (reason == null) return NotFound();
            return View(reason);
        }

        // POST: Reasons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Description,Type")] Reason reason)
        {
            if (id != reason.Id) return NotFound();

            if (reason.Name != null) reason.Name = reason.Name.Trim();
            if (reason.Description != null) reason.Description = reason.Description.Trim();

            var existing = await _mongoDbService.Reasons.Find(r => r.Name.ToLower() == reason.Name.ToLower() && r.Id != id).FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("Name", "This reason already exists.");
            }

            if (ModelState.IsValid)
            {
                await _mongoDbService.Reasons.ReplaceOneAsync(r => r.Id == id, reason);
                return RedirectToAction(nameof(Index));
            }
            return View(reason);
        }

        // GET: Reasons/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var reason = await _mongoDbService.Reasons.Find(r => r.Id == id).FirstOrDefaultAsync();
            if (reason == null) return NotFound();
            return View(reason);
        }

        // POST: Reasons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _mongoDbService.Reasons.DeleteOneAsync(r => r.Id == id);
            return RedirectToAction(nameof(Index));
        }
    }
}