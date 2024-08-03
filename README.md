<details>
<summary>Koşullar</summary>
:white_check_mark: Kütüphanenin .net 8 ile yazılması gerekmektedir. :tada: <br>
:white_check_mark: Kütüphane storage’a erişemediğinde son başarılı konfigürasyon kayıtları ile çalışabilmelidir. :tada:<br>
:white_check_mark: Kütüphane her tipe ait dönüş bilgisini kendi içerisinde halletmelidir. :tada:<br>
:white_check_mark: Sistem parametrik olarak verilen süre periyodunda yeni kayıtları ve kayıt değişikliklerini kontrol etmelidir. :tada:<br>
:white_check_mark: Her servis yalnızca kendi konfigürasyon kayıtlarına erişebilmeli, başkasının kayıtlarını görmemelidir. :tada:<br>
<br>
</details>
<details>
<summary>Ekstra Puan</summary>
:white_check_mark: Message Broker kullanılması. :tada: <br>
:white_check_mark: TPL, async/await kullanılması :tada:<br>
:white_check_mark: Olası concurrency problemlerini engelleyecek yapı kurgulanması :tada:<br>
:white_check_mark: Design & Architectural Pattern'lerin kullanılması. :tada:<br>
:white_check_mark: Gönderilen kod’un TDD yazılması. :tada:<br>
:white_check_mark: Gönderilen kodda Unit testlerin bulunması. :tada:<br
:white_check_mark: Storage yapısı olarak MongoDb, Redis gibi yapıların kullanılması. :tada:<br>
:white_check_mark: Projenin çalışır halde gönderilmesi. :tada:<br>
:negative_squared_cross_mark: Proje dokümantasyonu. :tada:<br>
:white_check_mark: Proje kodunun bir source control üzerinden paylaşılması (Github, Bitbucket vs). :tada:<br>
:white_check_mark: Tüm ekosistemin docker-compose ile çalıştırılabilir olması. :tada:<br>                                                                  
<br>
</details>
<details>
<summary>Proje Çalıştırma</summary>
  Docker Container'ları çalıştırmak için;

  ServicaA Projesini terminalde açın veya

  ```console
     cd ServiceA/ServiceA
  ```
dizinine gidin ve

```console
   docker-compose up
```
komutunu çalıştırın

</details>
<details>
<summary><strong>ConfigurationReaderLibrary Project</strong></summary><br>
Bu kütüphane MongoDb üzerinden servislerin ayarlarını istenilen süre aralığında reload etmek için yazılmıştır.
Kütüphane redis implementasyonuna sahiptir. MongoDb erişim problemlerinde Redis'deki cache kullanılır.<br><br>
  
```c#
   //MongoDB için database ve collection bilgileri
   private readonly string _applicationName;
   private readonly string _connectionString;
   private readonly IMongoCollection<ConfigurationSetting> _collection;
```
```c#
  //Ne kadar sürede yenileneceğini ayarlamak için timer ve süresi
  private readonly int _refreshTimerIntervalInMs;
  private readonly Timer _refreshTimer;
```
```c#
  //Database erişiminde sıkıntı çıkarsa son datanın alınması için redis ayarlamaları
  private readonly IDatabase _redisDatabase;
  private const string RedisKeyPrefix = "config:";
  private ConcurrentDictionary<string, ConfigurationSetting> _settings;
```

```c#
  // Task işlemleri için aynı anda kaç tane taskın çalışacağına dair ayarlama nesnesi
  private readonly SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);
```
```c#
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
```
```c#
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
```
```c#
  // İstenilen sürede tekrar olarak ayarları kontrol etme
  public async void OnTimerElapsed(object state)
  {
    await LoadSettings();
  }
```
```c#
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
```
</details>
<details>
<summary><strong>ServiceA Project</strong></summary><br>
ConfigurationReaderLibrary projesinde bulunan "ConfigurationReader" nesnesini kullanarak kendine ait settingleri kullanan bir web servis projesidir.Proje içerisinde kullanınan MediaTr implementasyonu ile TDD mimarisi kazandırılmıştır.<br><br>
<strong>Program.cs</strong>

```c#
   // MediatR kütüphanesinin entegrasyonu
   builder.Services.AddMediatR(configuration =>
   {
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
   });
```
```c#
   // ConfigurationReader sınıfının bir örneğinin tekil (singleton) bir hizmet olarak eklenmesi
    builder.Services.AddSingleton(new ConfigurationReader(
      applicationName: "SERVICE-A",
      connectionString: "mongodb://root:example@localhost:27017/",
      refreshTimerIntervalInMs: 60000 // 60 saniye
    ));
```
<strong>GetByNameConfigurationValueQuery.cs</strong>
```c#
    //Bu kod, MediatR kullanarak bir sorgu işleyici tanımlar. GetByNameConfigurationValueQuery adlı sınıf, isme göre yapılandırma değeri sorgusu yapar ve GetByNameConfigurationResponse döner.
    //İç içe geçen GetByNameConfigurationValueQueryHandler sınıfı, ConfigurationReader'ı kullanarak yapılandırma değerini alır, işler ve sonuç olarak sorgunun cevabını döner.
  public class GetByNameConfigurationValueQuery:IRequest<GetByNameConfigurationResponse>
  {
    public  string Name { get; set; }
    public class GetByNameConfigurationValueQueryHandler : IRequestHandler<GetByNameConfigurationValueQuery, GetByNameConfigurationResponse>
    {
        private readonly ConfigurationReader _configurationReader;

        public GetByNameConfigurationValueQueryHandler( ConfigurationReader configurationReader)
        {
            _configurationReader = configurationReader;
        }

        public async Task<GetByNameConfigurationResponse> Handle(GetByNameConfigurationValueQuery request, CancellationToken cancellationToken)
        {
            var value = _configurationReader.GetValue<dynamic> (request.Name);
            object newValue = ValueParser.ParseString(value);
            GetByNameConfigurationResponse result=new GetByNameConfigurationResponse() { Name=request.Name,Value= newValue };
            return result;
        }
    }
  }

```
<strong>BaseController.cs</strong>
```c#
  //Bu kod, ASP.NET Core'da BaseController adında bir temel denetleyici tanımlar ve MediatR'ı kullanmak için bir IMediator özelliği sağlar. Mediator özelliği, ilk kez kullanıldığında IMediator örneğini HttpContext üzerinden alır ve bu sayede MediatR isteklerini yönetir.
  public class BaseController : ControllerBase
  {
      private IMediator? _mediator;
      protected IMediator? Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();

     
  }
```
</details>
  <details>
<summary><strong>ServiceB Project</strong></summary><br>
ConfigurationReaderLibrary projesinde bulunan "ConfigurationReader" nesnesini kullanarak kendine ait settingleri kullanan ikinci* web servis projesidir.Proje içerisinde kullanınan MediaTr implementasyonu ile TDD mimarisi kazandırılmıştır.<br><br>
<strong>Program.cs</strong>

```c#
   // MediatR kütüphanesinin entegrasyonu
   builder.Services.AddMediatR(configuration =>
   {
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
   });
```
```c#
   // ConfigurationReader sınıfının bir örneğinin tekil (singleton) bir hizmet olarak eklenmesi
    builder.Services.AddSingleton(new ConfigurationReader(
      applicationName: "SERVICE-B",
      connectionString: "mongodb://root:example@localhost:27017/",
      refreshTimerIntervalInMs: 60000 // 60 saniye
    ));
```
<strong>GetByNameConfigurationValueQuery.cs</strong>
```c#
    //Bu kod, MediatR kullanarak bir sorgu işleyici tanımlar. GetByNameConfigurationValueQuery adlı sınıf, isme göre yapılandırma değeri sorgusu yapar ve GetByNameConfigurationResponse döner.
    //İç içe geçen GetByNameConfigurationValueQueryHandler sınıfı, ConfigurationReader'ı kullanarak yapılandırma değerini alır, işler ve sonuç olarak sorgunun cevabını döner.
  public class GetByNameConfigurationValueQuery:IRequest<GetByNameConfigurationResponse>
  {
    public  string Name { get; set; }
    public class GetByNameConfigurationValueQueryHandler : IRequestHandler<GetByNameConfigurationValueQuery, GetByNameConfigurationResponse>
    {
        private readonly ConfigurationReader _configurationReader;

        public GetByNameConfigurationValueQueryHandler( ConfigurationReader configurationReader)
        {
            _configurationReader = configurationReader;
        }

        public async Task<GetByNameConfigurationResponse> Handle(GetByNameConfigurationValueQuery request, CancellationToken cancellationToken)
        {
            var value = _configurationReader.GetValue<dynamic> (request.Name);
            object newValue = ValueParser.ParseString(value);
            GetByNameConfigurationResponse result=new GetByNameConfigurationResponse() { Name=request.Name,Value= newValue };
            return result;
        }
    }
  }

```
<strong>BaseController.cs</strong>
```c#
  //Bu kod, ASP.NET Core'da BaseController adında bir temel denetleyici tanımlar ve MediatR'ı kullanmak için bir IMediator özelliği sağlar. Mediator özelliği, ilk kez kullanıldığında IMediator örneğini HttpContext üzerinden alır ve bu sayede MediatR isteklerini yönetir.
  public class BaseController : ControllerBase
  {
      private IMediator? _mediator;
      protected IMediator? Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();

     
  }
```
</details>
<details>
<summary><strong>ConfigurationWebPage Project</strong></summary><br>
Bu proje MongoDb üzerinde bulunan Configuration değerleri için bir kullanıcı web projesidir. Kullanıcı tüm CRUD işlemlerini yapabilir. Client tarafında isme göre filtreleme yapabilir. Sistem MongoDb,Redis ve RabbitMQ entegrasyonları içermektedir. TDD yaklaşımı içinde MediaTr implementasyonları yapılmıştır. Redis ve RabbitMQ işlemleri MediaTr Pipeline olarak eklenmiştir.Tanımlanan CRUD işlemlerinin hepsine MediaTr entegrasyonu yapılmıştır. Cache ve MessageBroker sistemler için hazırlanan alt yapı inherit edilerek kullanılmaktadır<br><br>
<strong>Program.cs</strong><br>
  
```c#
    //MongoDB entegre edilmesi
    builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));
```
```c#
    //MediaTr implementasyonu pipeline ayarlamaları
    builder.Services.AddMediatR(configuration =>
  {
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
    configuration.AddOpenBehavior(typeof(CacheRemovingBehavior<,>));
    configuration.AddOpenBehavior(typeof(RabbitMQServiceBehavior<,>));

  
  });
```
```c#
    //Redis implementasyonu
    builder.Services.AddStackExchangeRedisCache(opt=>opt.Configuration="localhost:6379");

```
<strong>CachingBehavior.cs</strong>
```c#
    //Bu kod, MediatR ile çalışan bir önbellekleme davranışını uygular. CachingBehavior<TRequest, TResponse> sınıfı, belirli türdeki istek ve yanıtları yönetmek için kullanılır ve ICachableRequest arayüzünü uygulayan isteklerin yanıtlarını önbelleğe alır.
    //İlk olarak, CachingBehavior sınıfı oluşturulduğunda, önbellek ayarları (CacheSettings) ve dağıtılmış önbellek hizmeti (IDistributedCache) yapılandırılır. Handle metodu, gelen istekleri işler. Eğer istek önbelleği atlamıyorsa (BypassCache false ise), önbellekte 
    //yanıt olup olmadığını kontrol eder. Yanıt önbellekte varsa, doğrudan oradan alır. Değilse, isteği işleyip yanıtı oluşturur ve bunu önbelleğe ekler.
    //Ayrıca, ObjectIdConverter sınıfı, MongoDB'deki ObjectId türündeki verileri JSON serileştirici için dönüştürür. Bu, ObjectId türünü JSON'a ve JSON'dan dönüştürmek için kullanılır.
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ICachableRequest
  {
    private readonly CacheSettings _cacheSettings;
    private readonly IDistributedCache _cache;

    public CachingBehavior( IDistributedCache cache,IConfiguration configuration)
    {
        _cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>()??throw new InvalidOperationException();
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
        {
            return await next();
        }
        TResponse response;
        byte[] cacheResponse = await _cache.GetAsync(request.CacheKey,cancellationToken);
        if (cacheResponse!=null)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new ObjectIdConverter() }
            };
            response =JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cacheResponse), options);
        }
        else
        {
            response = await getResponseAndAddToCache(request,next,cancellationToken);
        }
        return response;
      }

      private async Task<TResponse?> getResponseAndAddToCache(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
      {
        TResponse response=await next();
        TimeSpan slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromHours(_cacheSettings.SlidingExpiration);
        DistributedCacheEntryOptions cacheEntryOptions = new() { 
        SlidingExpiration = slidingExpiration,
        };
        var options = new JsonSerializerOptions
        {
            Converters =  {   new ObjectIdConverter()  }
        };
        byte[] serializedData=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, options));
        await _cache.SetAsync(request.CacheKey,serializedData, cacheEntryOptions,cancellationToken);

        return response;
    }
  }
  public class ObjectIdConverter : JsonConverter<ObjectId>
  {
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return string.IsNullOrWhiteSpace(stringValue) ? ObjectId.Empty : ObjectId.Parse(stringValue);
    }

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
  }

```
<strong>CacheRemovingBehavior.cs</strong>
```c#
    //Bu kod, MediatR kullanarak önbellekteki verileri temizleyen bir davranış tanımlar. CacheRemovingBehavior<TRequest, TResponse> sınıfı, istek ve yanıtları ele alırken, belirli koşullarda önbellekteki kayıtları silmek için kullanılır. Sınıf, IDistributedCache     
   //kullanarak önbelleğe erişir. Handle metodu, gelen isteği işlerken önce BypassCache özelliğini kontrol eder. Eğer BypassCache true ise, önbelleği atlar ve işlemi doğrudan gerçekleştirir. İstek işlendikten sonra, eğer CacheKey null değilse, önbellekte belirtilen 
   //anahtara karşılık gelen veri silinir. Bu, örneğin bir güncelleme işlemi sonrası önbellekteki eski verileri temizlemek için kullanılır. Bu sayede, uygulama güncel olmayan verileri önbellekten çekmez ve verilerin güncel kalması sağlanır.
  
  public class CacheRemovingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ICacheRemoverRequest
  {
    private readonly IDistributedCache _cache;

    public CacheRemovingBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.BypassCache)
        {
            return await next();
        }
        TResponse response=await next();
        if (request.CacheKey!=null)
        {
            await _cache.RemoveAsync(request.CacheKey,cancellationToken);
        }
        return response;
    }
  }
```
<strong>RabbitMQServiceBehavior.cs</strong>
```c#
   //Bu kod, MediatR kullanarak RabbitMQ'ya mesaj gönderen bir davranış tanımlar. RabbitMQServiceBehavior<TRequest, TResponse> sınıfı, bir MediatR isteği işlendikten sonra yanıtı RabbitMQ kuyruğuna gönderir.İlk olarak, RabbitMQ'ya bağlanmak için gerekli ayarlar 
   //(hostname, username, password) konfigürasyondan alınır. Handle metodunda, istek işlendikten sonra yanıt alınır. Bu yanıt, RabbitMQ kuyruğuna JSON formatında serileştirilir ve gönderilir. Kuyruk, istekten alınan kuyruk adıyla tanımlanır veya mevcutsa kullanılır. Bu 
   //işlem, yanıtların RabbitMQ üzerinden diğer sistemlere iletilmesini sağlar.
  public class RabbitMQServiceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, IRabbitMQService
  {

    private readonly RabbitMQServiceSettings _rabbitMQServiceSettings;

    public RabbitMQServiceBehavior(IConfiguration configuration)
    {
        _rabbitMQServiceSettings = configuration.GetSection("RabbitMQServiceSettings").Get<RabbitMQServiceSettings>() ?? throw new InvalidOperationException();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        TResponse response = await next();
        var factory = new ConnectionFactory { HostName = _rabbitMQServiceSettings.hostname ,UserName= _rabbitMQServiceSettings.username,Password= _rabbitMQServiceSettings.password};
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: request.quequeName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

        channel.BasicPublish(exchange: string.Empty,
                             routingKey:request.quequeName,
                             basicProperties: null,
                             body: body);
        return await next();
    }

  }

```
</details>

<details>
<summary><strong>ConfigurationReaderLibrary.Test Project</strong></summary><br>
Bu kod, ConfigurationReader sınıfının işlevselliğini test eden iki birim testini içerir.

ServiceASiteNameTest:

ConfigurationReader sınıfının "SERVICE-A" yapılandırması için SiteName anahtarının değerini test eder.
Bu test, GetValue<string>("SiteName") metodunun doğru bir şekilde "soty.io" değerini döndürüp döndürmediğini kontrol eder.
ServiceBSiteNameTest:

ConfigurationReader sınıfının "SERVICE-B" yapılandırması için SiteName anahtarının değerini test eder.
Bu test, GetValue<string>("SiteName") metodunun SiteName anahtarının mevcut olmadığı durumda bir istisna fırlatıp fırlatmadığını kontrol eder.
Bu testler, ConfigurationReader sınıfının doğru yapılandırma değerlerini döndürüp döndürmediğini ve uygun hata yönetimini sağladığını doğrular.<br><br>
<strong>UnitTest1.cs</strong><br>
```c#
   public class UnitTest1
   {
     [Fact]
     public void ServiceASiteNameTest()
     {
         ConfigurationReader configurationReader=new ConfigurationReader("SERVICE-A", "mongodb://root:example@localhost:27017/",10);
         string result = configurationReader.GetValue<string>("SiteName");
         Assert.Equal("soty.io", result);
     }  

     [Fact]
     public void ServiceBSiteNameTest()
     {
         ConfigurationReader configurationReader=new ConfigurationReader("SERVICE-B", "mongodb://root:example@localhost:27017/",10);
         Assert.Throws<Exception>(() => configurationReader.GetValue<string>("SiteName"));
     }
   }
```
</details>
