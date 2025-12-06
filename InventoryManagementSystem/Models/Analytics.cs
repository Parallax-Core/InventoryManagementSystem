namespace InventoryManagementSystem.Models
{
    public class Analytics
    {
        // --- Summary Cards Data ---
        public int TotalProducts { get; set; }
        public int CategoryCount { get; set; }
        public int SupplierCount { get; set; }
        public int LowStockCount { get; set; }
        public int TotalStockOutThisMonth { get; set; }
        public decimal EstimatedInventoryValue { get; set; }

        // --- Chart 1: Top Selling Products ---
        public List<string> TopProductNames { get; set; } = new List<string>();
        public List<int> TopProductSales { get; set; } = new List<int>();

        // --- Chart 2: Stock Out Reasons ---
        public List<string> ReasonLabels { get; set; } = new List<string>();
        public List<int> ReasonCounts { get; set; } = new List<int>();
    }
}