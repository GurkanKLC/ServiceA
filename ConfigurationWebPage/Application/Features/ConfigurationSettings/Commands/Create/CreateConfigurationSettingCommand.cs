using ConfigurationWebPage.Application.Pipelines.Caching;
using ConfigurationWebPage.Application.Pipelines.MessageBroker;
using ConfigurationWebPage.Models;
using ConfigurationWebPage.Services;
using MediatR;

namespace ConfigurationWebPage.Application.Features.ConfigurationSettings.Commands.Create
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configurationSettingDto"></param>
    /// <returns>CreateConfigurationSettingResponse</returns>
    /// <summary>
    /// Veri Tabanına yeni bir setting eklemek için kullanılır
    /// </summary>
    public class CreateConfigurationSettingCommand : IRequest<CreateConfigurationSettingResponse>, ICacheRemoverRequest, IRabbitMQService
    {
        public required ConfigurationSettingDto configurationSettingDto { get; set; }

        public string CacheKey => "GetListConfigurationSettingQuery";

        public bool BypassCache  {get;}

        public string quequeName => "CreateConfigurationSettingCommand";

        public class CreateConfigurationSettingCommandHandler : IRequestHandler<CreateConfigurationSettingCommand, CreateConfigurationSettingResponse>
        {
            private readonly ConfigurationService _configurationService;

            public CreateConfigurationSettingCommandHandler(ConfigurationService configurationService)
            {
                _configurationService = configurationService;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public async Task<CreateConfigurationSettingResponse>? Handle(CreateConfigurationSettingCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    ConfigurationSetting configurationSetting = new()
                    {
                        ApplicationName = request.configurationSettingDto.ApplicationName,
                        IsActive = request.configurationSettingDto.IsActive,
                        Name = request.configurationSettingDto.Name,
                        Type = request.configurationSettingDto.Type,
                        Value = request.configurationSettingDto.Value,
                    };
                    await _configurationService.CreateAsync(configurationSetting);

                    return new CreateConfigurationSettingResponse() { IsSuccess = true };
                }
                catch
                (Exception ex)
                {
                    return new CreateConfigurationSettingResponse() { IsSuccess = false };

                }
            }
        }
    }
}
