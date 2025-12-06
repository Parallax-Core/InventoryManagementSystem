using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    [BsonIgnoreExtraElements]
    public class StockMovement
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("productId")]
        public string ProductId { get; set; }

        [BsonElement("quantityChange")]
        public int QuantityChange { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("remarks")]
        public string? Remarks { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        // *** FIX: Made UserName nullable ***
        [BsonElement("userName")]
        public string? UserName { get; set; } // This field is set by the controller, not the form.

        // Helper property, not stored in DB
        [BsonIgnore]
        public Product? Product { get; set; }
    }
}

