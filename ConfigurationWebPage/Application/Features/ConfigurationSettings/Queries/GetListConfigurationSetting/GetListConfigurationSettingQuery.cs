using ConfigurationWebPage.Application.Pipelines.Caching;
using ConfigurationWebPage.Models;
using ConfigurationWebPage.Services;
using MediatR;

namespace ConfigurationWebPage.Application.Features.ConfigurationSettings.Queries.GetListConfigurationSetting
{
    public class GetListConfigurationSettingQuery:IRequest<List<ConfigurationSetting>>,ICachableRequest
    {
        
        public TimeSpan? SlidingExpiration {  get;}

        string ICachableRequest.CacheKey => "GetListConfigurationSettingQuery";

        bool ICachableRequest.BypassCache {  get;}

    public class GetListConfigurationSettingQueryHandler : IRequestHandler<GetListConfigurationSettingQuery, List<ConfigurationSetting>>
        {
            private readonly ConfigurationService _configurationService;

            public GetListConfigurationSettingQueryHandler(ConfigurationService configurationService)
            {
                _configurationService = configurationService;
            }

            public async Task<List<ConfigurationSetting>> Handle(GetListConfigurationSettingQuery request, CancellationToken cancellationToken)
            {
               
                return await _configurationService.GetAllAsync(); ;
            }
        }
    }
}
