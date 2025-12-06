using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    [BsonIgnoreExtraElements]
    public class Reason
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        [BsonElement("name")]
        [Display(Name = "Reason Name")]
        public string Name { get; set; } = null!;

        [BsonElement("description")]
        public string? Description { get; set; }

        // Optional: You can add a type to categorize reasons if needed (e.g., "In", "Out")
        [BsonElement("type")]
        public string? Type { get; set; }
    }
}