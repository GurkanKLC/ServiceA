using ConfigurationWebPage.Models;
using ConfigurationWebPage.Services;
using MediatR;
using MongoDB.Bson;

namespace ConfigurationWebPage.Application.Features.ConfigurationSettings.Queries.GetById
{
    public class GetByIdConfigurationSettingQuery : IRequest<ConfigurationSettingResponse>
    {
        public ObjectId Id { get; set; }
        public class GetByIdConfigurationSettingQueryHandler : IRequestHandler<GetByIdConfigurationSettingQuery, ConfigurationSettingResponse>
        {
            private readonly ConfigurationService _configurationService;

            public GetByIdConfigurationSettingQueryHandler(ConfigurationService configurationService)
            {
                _configurationService = configurationService;
            }

            public async Task<ConfigurationSettingResponse> Handle(GetByIdConfigurationSettingQuery request, CancellationToken cancellationToken)
            {
                ConfigurationSetting result =await _configurationService.GetAsync(request.Id);
              
                ConfigurationSettingResponse response = new()
                {
                    ApplicationName=result.ApplicationName,
                    Id = result.Id,
                    IsActive = result.IsActive,
                    Name = result.Name, 
                    Type = result.Type,
                    Value = result.Value
                };
                return response;
                
            }
        }
    }
}
