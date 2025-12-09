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

        // GET: /StockMovement/History/{id}
        public async Task<IActionResult> History(string id)
        {
            if (id == null) return NotFound();

            var product = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null) return NotFound();

            ViewBag.Product = product;

            // 1. Fetch the raw movements from the database
            var movements = await _mongoDbService.StockMovements
                .Find(m => m.ProductId == id)
                .SortByDescending(m => m.Timestamp)
                .ToListAsync();

            // 2. *** THE FIX: Manual Lookup ***
            // We must loop through each movement and fetch the Reason details
            // using the ReasonId stored in the movement.
            foreach (var move in movements)
            {
                if (!string.IsNullOrEmpty(move.ReasonId))
                {
                    move.Reason = await _mongoDbService.Reasons
                        .Find(r => r.Id == move.ReasonId)
                        .FirstOrDefaultAsync();
                }
            }

            return View(movements);
        }

        // GET: /StockMovement/StockIn
        public async Task<IActionResult> StockIn()
        {
            await PopulateProductsDropdown();
            await PopulateReasonsDropdown("In");
            return View();
        }

        // POST: /StockMovement/StockIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(StockMovement movement, int quantityIn)
        {
            // Validation: Quantity
            if (quantityIn <= 0) ModelState.AddModelError("quantityIn", "Quantity to add must be greater than 0.");

            // Validation: Product
            var product = await _mongoDbService.Products.Find(p => p.Id == movement.ProductId).FirstOrDefaultAsync();
            if (product == null) ModelState.AddModelError("ProductId", "Product not found.");

            if (ModelState.IsValid)
            {
                // Update Product Stock
                product.Quantity += quantityIn;
                product.LastModifiedBy = User.Identity.Name;
                product.LastModifiedAt = System.DateTime.UtcNow;
                await _mongoDbService.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

                // Create Movement Record
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
            await PopulateReasonsDropdown("Out");
            return View();
        }

        // POST: /StockMovement/StockOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(StockMovement movement, int quantityOut)
        {
            if (quantityOut <= 0) ModelState.AddModelError("quantityOut", "Quantity to remove must be greater than 0.");

            var product = await _mongoDbService.Products.Find(p => p.Id == movement.ProductId).FirstOrDefaultAsync();
            if (product == null) ModelState.AddModelError("ProductId", "Product not found.");
            else if (product.Quantity < quantityOut) ModelState.AddModelError("quantityOut", $"Not enough stock. Current quantity: {product.Quantity}");

            if (ModelState.IsValid)
            {
                // Update Product Stock
                product.Quantity -= quantityOut;
                product.LastModifiedBy = User.Identity.Name;
                product.LastModifiedAt = System.DateTime.UtcNow;
                await _mongoDbService.Products.ReplaceOneAsync(p => p.Id == product.Id, product);

                // Create Movement Record
                movement.QuantityChange = -quantityOut; // Negative for OUT
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

        private async Task PopulateReasonsDropdown(string type)
        {
            var filter = Builders<Reason>.Filter.Or(
                Builders<Reason>.Filter.Eq(r => r.Type, type),
                Builders<Reason>.Filter.Eq(r => r.Type, "Both"),
                Builders<Reason>.Filter.Eq(r => r.Type, null)
            );

            var reasons = await _mongoDbService.Reasons.Find(filter).ToListAsync();

            // IMPORTANT: Sending "Id" as the value, "Name" as the text
            ViewBag.Reasons = new SelectList(reasons, "Id", "Name");
        }
    }
}