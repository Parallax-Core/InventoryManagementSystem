using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
            var viewModel = new Analytics();

            // 1. Summary Cards Logic
            var allProducts = await _mongoDbService.Products.Find(p => true).ToListAsync();
            var allCategories = await _mongoDbService.Categories.Find(c => true).ToListAsync();
            var allSuppliers = await _mongoDbService.Suppliers.Find(s => true).ToListAsync();

            viewModel.TotalProducts = allProducts.Count;
            viewModel.LowStockCount = allProducts.Count(p => p.Quantity < 10); // Assuming 10 is low stock threshold
            viewModel.EstimatedInventoryValue = allProducts.Sum(p => p.Price * p.Quantity);
            viewModel.CategoryCount = allCategories.Count;
            viewModel.SupplierCount = allSuppliers.Count;

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Fetch movements for this month (negative quantity = stock out)
            var monthlyStockOuts = await _mongoDbService.StockMovements
                .Find(m => m.Timestamp >= startOfMonth && m.QuantityChange < 0)
                .ToListAsync();

            viewModel.TotalStockOutThisMonth = monthlyStockOuts.Sum(m => Math.Abs(m.QuantityChange));


            // 2. Top Selling Products Logic (Based on "Sale" Stock Outs)
            // We filter for stock movements where Type is "Sale" OR "Stock Out" (adjust string if needed)
            // and where quantity is negative.
            var salesMovements = await _mongoDbService.StockMovements
                .Find(m => (m.Type == "Sale" || m.Type == "Stock Out") && m.QuantityChange < 0)
                .ToListAsync();

            var topSales = salesMovements
                .GroupBy(m => m.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(m => Math.Abs(m.QuantityChange))
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5) // Top 5
                .ToList();

            foreach (var item in topSales)
            {
                var product = await _mongoDbService.Products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                // Use product name if found, otherwise "Unknown" (in case product was deleted)
                viewModel.TopProductNames.Add(product?.Name ?? "Unknown Product");
                viewModel.TopProductSales.Add(item.TotalQuantity);
            }


            // 3. Reasons Breakdown Logic (Pie Chart)
            // We look at ALL stock outs (negative change) to see why items are leaving
            var allStockOuts = await _mongoDbService.StockMovements
                .Find(m => m.QuantityChange < 0)
                .ToListAsync();

            var reasons = allStockOuts
                .GroupBy(m => m.Type)
                .Select(g => new {
                    Reason = g.Key,
                    Count = g.Sum(m => Math.Abs(m.QuantityChange))
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            foreach (var r in reasons)
            {
                viewModel.ReasonLabels.Add(r.Reason ?? "Unspecified");
                viewModel.ReasonCounts.Add(r.Count);
            }

            return View(viewModel);
        }
    }
}