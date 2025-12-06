using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Diagnostics;

namespace InventoryManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly MongoDbService _mongoDbService;

        public DashboardController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.ProductCount = await _mongoDbService.Products.CountDocumentsAsync(new BsonDocument());
            ViewBag.CategoryCount = await _mongoDbService.Categories.CountDocumentsAsync(new BsonDocument());
            ViewBag.SupplierCount = await _mongoDbService.Suppliers.CountDocumentsAsync(new BsonDocument());
            return View();
        }
    }
}

