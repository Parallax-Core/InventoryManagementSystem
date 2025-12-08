using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        public async Task<IActionResult> Dashboard() // Or "Dashboard" depending on your route
        {
            // 1. Fetch Data
            var products = await _mongoDbService.Products.Find(_ => true).ToListAsync();
            var movements = await _mongoDbService.StockMovements.Find(_ => true).ToListAsync();
            var reasons = await _mongoDbService.Reasons.Find(_ => true).ToListAsync(); // *** NEW: Fetch Reasons ***
            var suppliers = await _mongoDbService.Suppliers.Find(_ => true).ToListAsync();

            // 2. Calculate Basic Metrics
            var analytics = new Analytics
            {
                TotalProducts = products.Count,
                CategoryCount = products.Select(p => p.CategoryId).Distinct().Count(),
                SupplierCount = suppliers.Count,
                LowStockCount = products.Count(p => p.Quantity < 10), // Assuming 10 is low stock threshold

                // Inventory Value = Sum(Price * Quantity)
                EstimatedInventoryValue = products.Sum(p => p.Price * p.Quantity),

                // Stock Out This Month = Sum of negative quantity changes in current month
                TotalStockOutThisMonth = movements
                .Where(m => m.QuantityChange < 0 && m.Timestamp.Month == System.DateTime.UtcNow.Month)
                .Sum(m => Math.Abs(m.QuantityChange))
            };

            // 3. Top Selling Products (Bar Chart)
            // Filter reasons to identify "Sales" (excluding "Expired", "Damaged", etc.)
            // matches logic: (m.Type == "Sale" || m.Type == "Stock Out")
            var saleReasonIds = reasons
                .Where(r => r.Name.Equals("Sale", System.StringComparison.OrdinalIgnoreCase) ||
                            r.Name.Equals("Stock Out", System.StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Id)
                .ToList();

            // Group negative movements by ProductId to see what's leaving stock via Sales
            var topProducts = movements
                .Where(m => m.QuantityChange < 0 && saleReasonIds.Contains(m.ReasonId))
                .GroupBy(m => m.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Sold = g.Sum(x => Math.Abs(x.QuantityChange))
                })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToList();

            // Map Product IDs to Names
            analytics.TopProductNames = topProducts
                .Select(x => products.FirstOrDefault(p => p.Id == x.ProductId)?.Name ?? "Unknown")
                .ToList();

            analytics.TopProductSales = topProducts.Select(x => x.Sold).ToList();

            // 4. Stock Out Reasons (Doughnut Chart)
            // *** FIX FOR ERROR CS1061 ***
            // Instead of GroupBy(m => m.Type), we GroupBy the Reason Name by looking up the ReasonId
            var stockOutMovements = movements.Where(m => m.QuantityChange < 0).ToList();

            var reasonStats = stockOutMovements
                .GroupBy(m =>
                {
                    // Find the reason object that matches the movement's ReasonId
                    var reason = reasons.FirstOrDefault(r => r.Id == m.ReasonId);
                    return reason != null ? reason.Name : "Uncategorized";
                })
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToList();

            analytics.ReasonLabels = reasonStats.Select(x => x.Label).ToList();
            analytics.ReasonCounts = reasonStats.Select(x => x.Count).ToList();

            return View(analytics);
        }
    }
}