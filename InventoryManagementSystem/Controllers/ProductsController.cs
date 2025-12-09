using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace InventoryManagementSystem.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly MongoDbService _mongoDbService;

    public ProductsController(MongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
    }

    // GET: Products
    public async Task<IActionResult> Index(string searchString, string statusFilter, string categoryFilter, string supplierFilter)
    {
        // Start with a base filter that matches everything
        var filterBuilder = Builders<Product>.Filter;
        var filter = filterBuilder.Empty;

        // 1. Apply Search Filter (Name)
        if (!string.IsNullOrEmpty(searchString))
        {
            // *** ADDED: Data Trimming for Search ***
            searchString = searchString.Trim();

            var searchRegex = new BsonRegularExpression(searchString, "i");
            filter &= filterBuilder.Regex(p => p.Name, searchRegex);
        }

        // 2. Apply Status Filter
        if (!string.IsNullOrEmpty(statusFilter))
        {
            if (statusFilter == "Active")
            {
                filter &= filterBuilder.Eq(p => p.IsActive, true);
            }
            else if (statusFilter == "Inactive")
            {
                filter &= filterBuilder.Eq(p => p.IsActive, false);
            }
        }

        // 3. Apply Category Filter
        if (!string.IsNullOrEmpty(categoryFilter))
        {
            filter &= filterBuilder.Eq(p => p.CategoryId, categoryFilter);
        }

        // 4. Apply Supplier Filter
        if (!string.IsNullOrEmpty(supplierFilter))
        {
            filter &= filterBuilder.Eq(p => p.SupplierId, supplierFilter);
        }

        // Fetch the filtered list
        var products = await _mongoDbService.Products.Find(filter).ToListAsync();

        // Populate related data (Category/Supplier) for display
        foreach (var product in products)
        {
            if (ObjectId.TryParse(product.CategoryId, out _))
            {
                product.Category = await _mongoDbService.Categories.Find(c => c.Id == product.CategoryId).FirstOrDefaultAsync();
            }
            if (ObjectId.TryParse(product.SupplierId, out _))
            {
                product.Supplier = await _mongoDbService.Suppliers.Find(s => s.Id == product.SupplierId).FirstOrDefaultAsync();
            }
        }

        // --- Prepare Data for Dropdowns ---
        var categories = await _mongoDbService.Categories.Find(_ => true).ToListAsync();
        var suppliers = await _mongoDbService.Suppliers.Find(_ => true).ToListAsync();

        ViewBag.CategoryList = new SelectList(categories, "Id", "Name", categoryFilter);
        ViewBag.SupplierList = new SelectList(suppliers, "Id", "Name", supplierFilter);

        // Pass current filter values back to view
        ViewData["CurrentFilter"] = searchString;
        ViewData["StatusFilter"] = statusFilter;
        ViewData["CategoryFilter"] = categoryFilter;
        ViewData["SupplierFilter"] = supplierFilter;

        return View(products);
    }

    // GET: Products/Details/5
    public async Task<IActionResult> Details(string id)
    {
        if (id == null) return NotFound();
        var product = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        if (ObjectId.TryParse(product.CategoryId, out _))
        {
            product.Category = await _mongoDbService.Categories.Find(c => c.Id == product.CategoryId).FirstOrDefaultAsync();
        }
        if (ObjectId.TryParse(product.SupplierId, out _))
        {
            product.Supplier = await _mongoDbService.Suppliers.Find(s => s.Id == product.SupplierId).FirstOrDefaultAsync();
        }

        return View(product);
    }

    // GET: Products/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View();
    }

    // POST: Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Quantity,Price,CategoryId,SupplierId")] Product product)
    {
        // *** ENSURE: Data Trimming is here ***
        if (product.Name != null) product.Name = product.Name.Trim();

        // Robust Duplicate Check using Regex (Case-Insensitive Exact Match)
        var nameFilter = Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(product.Name)}$", "i"));
        var existing = await _mongoDbService.Products.Find(nameFilter).FirstOrDefaultAsync();

        if (existing != null)
        {
            ModelState.AddModelError("Name", "A product with this name already exists.");
        }

        if (ModelState.IsValid)
        {
            product.IsActive = true;
            product.CreatedBy = User.Identity.Name;
            product.CreatedAt = DateTime.UtcNow;
            product.LastModifiedBy = User.Identity.Name;
            product.LastModifiedAt = DateTime.UtcNow;

            await _mongoDbService.Products.InsertOneAsync(product);

            // 1. Initial Stock Logic
            var initialReason = await _mongoDbService.Reasons
                .Find(r => r.Name == "Initial Stock")
                .FirstOrDefaultAsync();

            if (initialReason == null)
            {
                initialReason = new Reason
                {
                    Name = "Initial Stock",
                    Type = "In",
                    Description = "System generated reason for new products"
                };
                await _mongoDbService.Reasons.InsertOneAsync(initialReason);
            }

            var movement = new StockMovement
            {
                ProductId = product.Id,
                QuantityChange = product.Quantity,
                ReasonId = initialReason.Id,
                Remarks = "Product created",
                Timestamp = DateTime.UtcNow,
                UserName = User.Identity.Name
            };
            await _mongoDbService.StockMovements.InsertOneAsync(movement);

            return RedirectToAction(nameof(Index));
        }
        await PopulateDropdowns();
        return View(product);
    }

    // GET: Products/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();
        var product = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        await PopulateDropdowns();
        return View(product);
    }

    // POST: Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Price,CategoryId,SupplierId,IsActive")] Product product)
    {
        if (id != product.Id) return NotFound();

        // *** ENSURE: Data Trimming is here ***
        if (product.Name != null) product.Name = product.Name.Trim();

        // Robust Duplicate Check using Regex (Case-Insensitive Exact Match) excluding current product
        var nameFilter = Builders<Product>.Filter.Regex(p => p.Name, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(product.Name)}$", "i"));
        var idFilter = Builders<Product>.Filter.Ne(p => p.Id, id);
        var combineFilter = Builders<Product>.Filter.And(nameFilter, idFilter);

        var existing = await _mongoDbService.Products.Find(combineFilter).FirstOrDefaultAsync();

        if (existing != null)
        {
            ModelState.AddModelError("Name", "A product with this name already exists.");
        }

        var productInDb = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (productInDb == null) return NotFound();

        if (ModelState.IsValid)
        {
            productInDb.Name = product.Name;
            productInDb.Price = product.Price;
            productInDb.CategoryId = product.CategoryId;
            productInDb.SupplierId = product.SupplierId;
            productInDb.IsActive = product.IsActive;

            productInDb.LastModifiedBy = User.Identity.Name;
            productInDb.LastModifiedAt = DateTime.UtcNow;

            await _mongoDbService.Products.ReplaceOneAsync(p => p.Id == id, productInDb);
            return RedirectToAction(nameof(Index));
        }
        await PopulateDropdowns();
        return View(product);
    }

    // GET: Products/ToggleStatus/5
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (id == null) return NotFound();
        var product = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null) return NotFound();

        if (ObjectId.TryParse(product.CategoryId, out _))
        {
            product.Category = await _mongoDbService.Categories.Find(c => c.Id == product.CategoryId).FirstOrDefaultAsync();
        }
        if (ObjectId.TryParse(product.SupplierId, out _))
        {
            product.Supplier = await _mongoDbService.Suppliers.Find(s => s.Id == product.SupplierId).FirstOrDefaultAsync();
        }

        return View(product);
    }

    // POST: Products/ToggleStatus/5
    [HttpPost, ActionName("ToggleStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatusConfirmed(string id)
    {
        var product = await _mongoDbService.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product != null)
        {
            product.IsActive = !product.IsActive;
            product.LastModifiedBy = User.Identity.Name;
            product.LastModifiedAt = DateTime.UtcNow;
            await _mongoDbService.Products.ReplaceOneAsync(p => p.Id == id, product);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Categories = new SelectList(await _mongoDbService.Categories.Find(c => c.IsActive == true).ToListAsync(), "Id", "Name");
        ViewBag.Suppliers = new SelectList(await _mongoDbService.Suppliers.Find(s => s.IsActive == true).ToListAsync(), "Id", "Name");
    }
}