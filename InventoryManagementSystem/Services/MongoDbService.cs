using InventoryManagementSystem.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InventoryManagementSystem.Services
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
    }

    public class MongoDbService
    {
        private readonly IMongoCollection<Product> _products;
        private readonly IMongoCollection<Category> _categories;
        private readonly IMongoCollection<Supplier> _suppliers;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<StockMovement> _stockMovements;
        private readonly IMongoCollection<Region> _regions;
        private readonly IMongoCollection<Province> _provinces;
        private readonly IMongoCollection<Municipality> _municipalities;
        private readonly IMongoCollection<Barangay> _barangays;
        private readonly IMongoCollection<Reason> _reasons;

        public MongoDbService(IOptions<MongoDbSettings> mongoDbSettings)
        {
            var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);

            _products = database.GetCollection<Product>("Products");
            _categories = database.GetCollection<Category>("Categories");
            _suppliers = database.GetCollection<Supplier>("Suppliers");
            _users = database.GetCollection<User>("Users");
            _stockMovements = database.GetCollection<StockMovement>("StockMovements");
            _regions = database.GetCollection<Region>("Region");
            _provinces = database.GetCollection<Province>("Province");
            _municipalities = database.GetCollection<Municipality>("Municipality");
            _barangays = database.GetCollection<Barangay>("Barangay");
            _reasons = database.GetCollection<Reason>("Reasons");
        }

        public IMongoCollection<Product> Products => _products;
        public IMongoCollection<Category> Categories => _categories;
        public IMongoCollection<Supplier> Suppliers => _suppliers;
        public IMongoCollection<User> Users => _users;
        public IMongoCollection<StockMovement> StockMovements => _stockMovements;
        public IMongoCollection<Region> Regions => _regions;
        public IMongoCollection<Province> Provinces => _provinces;
        public IMongoCollection<Municipality> Municipalities => _municipalities;
        public IMongoCollection<Barangay> Barangays => _barangays;
        public IMongoCollection<Reason> Reasons => _reasons;
    }
}