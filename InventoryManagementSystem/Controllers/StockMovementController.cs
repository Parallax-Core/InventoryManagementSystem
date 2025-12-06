using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Controllers
{
    [Authorize]
    public class StockMovementController : Controller
    {
        private readonly MongoDbService _mongoDbService;

        public StockMovementController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // ... (History method remains the same) ...
        public async Task<IActionResult> History(string id)
        {
            if (id == null) return NotFound();
            var product = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null) return NotFound();
            ViewBag.Product = product;
            var movements = await _mongoDbService.StockMovements
                .Find(m => m.ProductId == id)
                .SortByDescending(m => m.Timestamp)
                .ToListAsync();
            return View(movements);
        }

        // GET: /StockMovement/StockIn
        public async Task<IActionResult> StockIn()
        {
            await PopulateProductsDropdown();
            // *** NEW: Populate Reasons Dropdown ***
            await PopulateReasonsDropdown("In");
            return View();
        }

        // POST: /StockMovement/StockIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(StockMovement movement, int quantityIn)
        {
            // ... (Validation logic same as before) ...
            if (quantityIn <= 0) ModelState.AddModelError("quantityIn", "Quantity to add must be greater than 0.");

            var product = await _mongoDbService.Products.Find(p => p.Id == movement.ProductId).FirstOrDefaultAsync();
            if (product == null) ModelState.AddModelError("ProductId", "Product not found.");

            if (ModelState.IsValid)
            {
                // Update Product
                product.Quantity += quantityIn;
                product.LastModifiedBy = User.Identity.Name;
                product.LastModifiedAt = System.DateTime.UtcNow;
                await _mongoDbService.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

                // Create Movement
                movement.QuantityChange = quantityIn;
                movement.Timestamp = System.DateTime.UtcNow;
                movement.UserName = User.Identity.Name;
                await _mongoDbService.StockMovements.InsertOneAsync(movement);

                TempData["SuccessMessage"] = $"Successfully added {quantityIn} of {product.Name} to stock.";
                return RedirectToAction("Index", "Products");
            }

            await PopulateProductsDropdown();
            await PopulateReasonsDropdown("In");
            return View(movement);
        }


        // GET: /StockMovement/StockOut
        public async Task<IActionResult> StockOut()
        {
            await PopulateProductsDropdown();
            // *** NEW: Populate Reasons Dropdown ***
            await PopulateReasonsDropdown("Out");
            return View();
        }

        // POST: /StockMovement/StockOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(StockMovement movement, int quantityOut)
        {
            // ... (Validation logic same as before) ...
            if (quantityOut <= 0) ModelState.AddModelError("quantityOut", "Quantity to remove must be greater than 0.");

            var product = await _mongoDbService.Products.Find(p => p.Id == movement.ProductId).FirstOrDefaultAsync();
            if (product == null) ModelState.AddModelError("ProductId", "Product not found.");
            else if (product.Quantity < quantityOut) ModelState.AddModelError("quantityOut", $"Not enough stock. Current quantity: {product.Quantity}");

            if (ModelState.IsValid)
            {
                product.Quantity -= quantityOut;
                product.LastModifiedBy = User.Identity.Name;
                product.LastModifiedAt = System.DateTime.UtcNow;
                await _mongoDbService.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

                movement.QuantityChange = -quantityOut;
                movement.Timestamp = System.DateTime.UtcNow;
                movement.UserName = User.Identity.Name;
                await _mongoDbService.StockMovements.InsertOneAsync(movement);

                TempData["SuccessMessage"] = $"Successfully removed {quantityOut} of {product.Name} from stock.";
                return RedirectToAction("Index", "Products");
            }

            await PopulateProductsDropdown();
            await PopulateReasonsDropdown("Out");
            return View(movement);
        }

        private async Task PopulateProductsDropdown()
        {
            var products = await _mongoDbService.Products.Find(p => p.IsActive == true).ToListAsync();
            ViewBag.Products = new SelectList(products, "Id", "Name");
        }

        // *** NEW Helper Method ***
        private async Task PopulateReasonsDropdown(string type)
        {
            // Fetch reasons that match the type ('In', 'Out', or 'Both')
            var filter = Builders<Reason>.Filter.Or(
                Builders<Reason>.Filter.Eq(r => r.Type, type),
                Builders<Reason>.Filter.Eq(r => r.Type, "Both"),
                Builders<Reason>.Filter.Eq(r => r.Type, null) // Include uncategorized for backward compatibility
            );

            var reasons = await _mongoDbService.Reasons.Find(filter).ToListAsync();
            ViewBag.Reasons = new SelectList(reasons, "Name", "Name");
        }
    }
}