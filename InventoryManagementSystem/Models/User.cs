using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryManagementSystem.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        // *** REPLACED FullName with FirstName and LastName ***
        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        // *** NEW: Read-only property for concatenation ***
        // This is not stored in the database and is used for claims
        [BsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}

