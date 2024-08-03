using ConfigurationReaderLibrary.Repository;
using MongoDB.Driver;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Concurrent;
using ConfigurationReaderLibrary.Entity;

namespace ConfigurationReaderLibrary
{
    public class ConfigurationReader : IConfigurationReader
    {
        //MongoDB için database ve collection bilgileri
        private readonly string _applicationName;
        private readonly string _connectionString;
        private readonly IMongoCollection<ConfigurationSetting> _collection;

        //Ne kadar sürede yenileneceğini ayarlamak için timer ve süresi
        private readonly int _refreshTimerIntervalInMs;
        private readonly Timer _refreshTimer;

        //Database erişiminde sıkıntı çıkarsa son datanın alınması için redis ayarlamaları
        private readonly IDatabase _redisDatabase;
        private const string RedisKeyPrefix = "config:";
        private ConcurrentDictionary<string, ConfigurationSetting> _settings;

        // Task işlemleri için aynı anda kaç tane taskın çalışacağına dair ayarlama nesnesi
        private readonly SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);


        //Verilerin set edilmesi için bir constructor
        public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
        {
            _applicationName = applicationName;
            _connectionString = connectionString;
            _refreshTimerIntervalInMs = refreshTimerIntervalInMs;

            //MongoDb kullanıcı oluşturulması
            var client = new MongoClient(_connectionString);

            //MongoDb üzerinde hangi database'in kullanıcılacağının seçilmesi
            var database = client.GetDatabase("ConfigurationDatabase");
            //MongoDb üzerinde hangi koleksiyonun kullanıcılacağının seçilmesi
            _collection = database.GetCollection<ConfigurationSetting>("ConfigurationCollection");

            _settings = new ConcurrentDictionary<string, ConfigurationSetting>();

            //Redis bağlatısı
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            _redisDatabase = redis.GetDatabase();

            //Aktif taskın tamamlanmasının beklenmesi için
            LoadSettings().Wait();
            _refreshTimer = new Timer(OnTimerElapsed, null, _refreshTimerIntervalInMs, _refreshTimerIntervalInMs);
        }

        public async Task LoadSettings()
        {
            await _loadLock.WaitAsync();
            try
            {
                //MongoDB içinde istenilen datanın alınması için filtre oluşturulması
                var filter = Builders<ConfigurationSetting>.Filter.And(
                    Builders<ConfigurationSetting>.Filter.Eq("ApplicationName", _applicationName),
                    Builders<ConfigurationSetting>.Filter.Eq("IsActive", true)
                );

                //Oluşturulan filtrenin koleksiyon üzerinde kullanılması
                var settingsList = await _collection.Find(filter).ToListAsync();

                //Elde edilen verilen oluşturulan listeye Key-Value olarak aktarılıyor
                _settings = new ConcurrentDictionary<string, ConfigurationSetting>(
                    settingsList.ToDictionary(s => s.Name.ToLower(), s => s)
                );

                //Redis de kullanılması için cachekey oluşturulması
                var cacheKey = RedisKeyPrefix + _applicationName;

                //Redis verilerin json nesnesinedönüştürülmesi
                var settingsJson = JsonSerializer.Serialize(_settings.Values);

                //Eğer Redis Database hazırsa
                if (_redisDatabase != null)
                {
                    //Son Alınan verileri redis e aktarır ve 1 saat boyunca korur
                    await _redisDatabase.StringSetAsync(cacheKey, settingsJson, TimeSpan.FromHours(1));
                }
                else
                {
                    throw new Exception("Redis database is not initialized.");
                }
            }
            finally
            {
                _loadLock.Release();
            }
        }

        // İstenilen sürede tekrar olarak ayarları kontrol etme
        public async void OnTimerElapsed(object state)
        {
            await LoadSettings();
        }

        public T GetValue<T>(string name)
        {
            //Veriyi redisden almak için gerekli olan cachekey oluşturulması
            var cacheKey = RedisKeyPrefix + _applicationName;
            //key ile verilerin alınması
            var settingsJson = _redisDatabase.StringGet(cacheKey);

            //Eğer veriler boş değil ise
            if (!settingsJson.IsNullOrEmpty)
            {
                //string olarak alınan veriler nesneye dönüştürülür
                var cachedSettings = JsonSerializer.Deserialize<List<ConfigurationSetting>>(settingsJson);

                //Elde edilen verilen oluşturulan listeye Key-Value olarak aktarılıyor
                _settings = new ConcurrentDictionary<string, ConfigurationSetting>(
                    cachedSettings.ToDictionary(s => s.Name.ToLower(), s => s)
                );

            }
            else
            {
                //Eğer nesne boş ise tekrar yüklemeyi çalıştır
                LoadSettings().Wait();
            }


            if (_settings.TryGetValue(name.ToLower(), out var setting))
            {
                //Elde edilen value'nin istenilen tipte return edilmesi

                return (T)Convert.ChangeType(setting.Value, typeof(T));
            }
            else
            {
                throw new Exception("Ayar bulunamadı veya aktif değil.");
            }
        }
       
    }
}
