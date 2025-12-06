using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    [BsonIgnoreExtraElements]
    public class Address
    {
        [BsonElement("region")]
        public string? Region { get; set; }

        [BsonElement("province")]
        public string? Province { get; set; }

        [BsonElement("city")]
        public string? City { get; set; }

        [BsonElement("barangay")]
        public string? Barangay { get; set; }

        [Required]
        [BsonElement("streetAddress")]
        [Display(Name = "Street / House No.")]
        public string? StreetAddress { get; set; }

        [BsonElement("postalCode")]
        public string? PostalCode { get; set; }

        // Helper to display full address as a string if needed
        public string FullAddress => $"{StreetAddress}, {Barangay}, {City}, {Province}, {Region}";
    }
}