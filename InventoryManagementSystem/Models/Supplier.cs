using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace InventoryManagementSystem.Models
{
    [BsonIgnoreExtraElements]
    public class Supplier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        [BsonElement("name")]
        public string? Name { get; set; }

        [Required]
        [Display(Name = "Company Phone")]
        [RegularExpression(@"^(((\+63|0)9\d{9})|(\+63 \d \d{3} \d{4})|(1800 \d{2} \d{3} \d{4}))$",
            ErrorMessage = "Invalid format. (e.g., 09xxxxxxxxx, +63 2 123 4567, or 1800 10 123 4567)")]
        [BsonElement("companyContactNum")]
        public string? CompanyContactNum { get; set; }

        // *** CHANGE THIS PROPERTY ***
        // Old: public string Address { get; set; }
        // New:
        [Required]
        [BsonElement("address")]
        public Address Address { get; set; } = new Address();

        [BsonElement("contactPersons")]
        public List<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();

        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }

        [BsonElement("lastModifiedAt")]
        public DateTime LastModifiedAt { get; set; }

        [BsonElement("lastModifiedBy")]
        public string? LastModifiedBy { get; set; }
    }
}