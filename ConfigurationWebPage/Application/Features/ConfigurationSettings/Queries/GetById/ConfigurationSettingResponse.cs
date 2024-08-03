using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ConfigurationWebPage.Application.Features.ConfigurationSettings.Queries.GetById
{
    public class ConfigurationSettingResponse
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Type")]
        public string Type { get; set; }

        [BsonElement("Value")]
        public string Value { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; }

        [BsonElement("ApplicationName")]
        public string ApplicationName { get; set; }
    }
}
