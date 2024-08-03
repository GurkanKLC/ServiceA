using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationReader_Library
{
    public class ConfigurationReader
    {
        private readonly string _applicationName;
        private readonly string _connectionString;
        private readonly IMongoCollection<ConfigurationSetting> _collection;
        private readonly int _refreshTimerIntervalInMs;
        private List<ConfigurationSetting> _settings;
        private readonly Timer _refreshTimer;

        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            //_applicationName = applicationName;
            //_refreshTimerIntervalInMs = refreshTimerIntervalInMs;
            //var client = new MongoClient(connectionString);
            //var database = client.GetDatabase("ConfigurationDatabase"); // Veritabanı adı
            //_collection = database.GetCollection<ConfigurationSetting>("ConfigurationCollection"); // Koleksiyon adı
            //_settings = new List<ConfigurationSetting>();
            //LoadSettings().Wait(); // İlk yükleme
            //_refreshTimer = new Timer(OnTimerElapsed, null, _refreshTimerIntervalInMs, _refreshTimerIntervalInMs);
            _applicationName = applicationName;
            _connectionString = connectionString; // MongoDB kimlik doğrulaması için bağlantı dizesi
            _refreshTimerIntervalInMs = refreshTimerIntervalInMs;
            var client = new MongoClient(_connectionString);
          
            var database = client.GetDatabase("ConfigurationDatabase");
            _collection = database.GetCollection<ConfigurationSetting>("ConfigurationCollection");
            _settings = new List<ConfigurationSetting>();
            LoadSettings().Wait();
            _refreshTimer = new Timer(OnTimerElapsed, null, _refreshTimerIntervalInMs, _refreshTimerIntervalInMs);

        }

        private async Task LoadSettings()
        {
            var filter = Builders<ConfigurationSetting>.Filter.And(
                Builders<ConfigurationSetting>.Filter.Eq("ApplicationName", _applicationName),
                Builders<ConfigurationSetting>.Filter.Eq("IsActive", true)
            );

            _settings = await _collection.Find(filter).ToListAsync();
        }

        private async void OnTimerElapsed(object state)
        {
            await LoadSettings(); // Belirli aralıklarla güncelle
        }

        public T GetSetting<T>(string name)
        {
            var setting = _settings.FirstOrDefault(s => s.Name.ToLower() == name.ToLower());
            if (setting == null)
                throw new Exception("Ayar bulunamadı veya aktif değil.");

            return (T)Convert.ChangeType(setting.Value, typeof(T));
        }
    }
}
