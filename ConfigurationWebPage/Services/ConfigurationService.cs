using ConfigurationWebPage.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConfigurationWebPage.Services
{
    public class ConfigurationService
    {
        private readonly IMongoCollection<ConfigurationSetting> _configurations;

        public ConfigurationService(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _configurations = database.GetCollection<ConfigurationSetting>(settings.Value.CollectionName);
        }

        public async Task<List<ConfigurationSetting>> GetAllAsync()
        {
            return await _configurations.Find(x => true).ToListAsync();
        }

        public async Task<ConfigurationSetting> GetAsync(ObjectId id)
        {
            return await _configurations.Find<ConfigurationSetting>(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(ConfigurationSetting configuration)
        {
            await _configurations.InsertOneAsync(configuration);
        }

        public async Task UpdateAsync(ObjectId id, ConfigurationSetting configuration)
        {
            await _configurations.ReplaceOneAsync(x => x.Id == id, configuration);
        }

        public async Task RemoveAsync(ObjectId id)
        {
            await _configurations.DeleteOneAsync(x => x.Id == id);
        }
    }
}
