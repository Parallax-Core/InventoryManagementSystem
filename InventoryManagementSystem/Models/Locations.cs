using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryManagementSystem.Models
{
    [BsonIgnoreExtraElements]
    public class Region
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("region_id")]
        public int RegionId { get; set; }

        [BsonElement("region_name")]
        public string Name { get; set; }

        [BsonElement("region_description")]
        public string Description { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Province
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("province_id")]
        public int ProvinceId { get; set; }

        [BsonElement("region_id")]
        public int RegionId { get; set; }

        [BsonElement("province_name")]
        public string Name { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Municipality
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("municipality_id")]
        public int MunicipalityId { get; set; }

        [BsonElement("province_id")]
        public int ProvinceId { get; set; }

        [BsonElement("municipality_name")]
        public string Name { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Barangay
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("barangay_id")]
        public int BarangayId { get; set; }

        [BsonElement("municipality_id")]
        public int MunicipalityId { get; set; }

        [BsonElement("barangay_name")]
        public string Name { get; set; }
    }
}