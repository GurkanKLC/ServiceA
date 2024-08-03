namespace ConfigurationWebPage.Application.Pipelines.Caching;

public interface ICachableRequest
{
    string CacheKey { get; }
    bool BypassCache { get; }
    TimeSpan? SlidingExpiration { get; }

}
