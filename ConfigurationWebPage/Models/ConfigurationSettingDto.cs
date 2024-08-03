using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ConfigurationWebPage.Models
{
    public class ConfigurationSettingDto
    {

        [BsonElement("Name"),Required]
        public string Name { get; set; }

        [BsonElement("Type"), Required]
        public string Type { get; set; }

        [BsonElement("Value"), Required]
        public string Value { get; set; }

        [BsonElement("IsActive"), Required]
        public bool IsActive { get; set; }

        [BsonElement("ApplicationName"), Required]
        public string ApplicationName { get; set; }
    }
}
