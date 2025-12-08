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
        public string? ProductId { get; set; }

        // *** REFACTOR: Changed "Type" string to "ReasonId" Reference ***
        // This stores the ObjectId of the Reason document (e.g., "60d5ec...")
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("reasonId")]
        public string? ReasonId { get; set; }

        [BsonElement("quantityChange")]
        public int QuantityChange { get; set; }

        [BsonElement("remarks")]
        public string? Remarks { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("userName")]
        public string? UserName { get; set; }

        // *** NAVIGATION PROPERTIES (Not stored in DB) ***

        [BsonIgnore]
        public Product? Product { get; set; }

        // This allows you to say "move.Reason.Name" in your View
        // It is populated manually in the Controller loop.
        [BsonIgnore]
        public Reason? Reason { get; set; }
    }
}