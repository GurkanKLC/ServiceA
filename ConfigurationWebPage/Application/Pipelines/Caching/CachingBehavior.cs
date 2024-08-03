using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConfigurationWebPage.Application.Pipelines.Caching;

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
