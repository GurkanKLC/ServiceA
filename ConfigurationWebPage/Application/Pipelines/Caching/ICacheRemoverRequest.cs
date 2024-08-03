namespace ConfigurationWebPage.Application.Pipelines.Caching;

public interface ICacheRemoverRequest
{
    string CacheKey { get; }
    bool BypassCache { get; }
}
