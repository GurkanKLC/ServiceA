using ConfigurationWebPage.Application.Features.ConfigurationSettings.Queries.GetById;
using ConfigurationWebPage.Application.Pipelines.Caching;
using ConfigurationWebPage.Application.Pipelines.MessageBroker;
using ConfigurationWebPage.Models;
using ConfigurationWebPage.Services;
using MediatR;
using MongoDB.Bson;

namespace ConfigurationWebPage.Application.Features.ConfigurationSettings.Commands.Update
{
    public class UpdateConfigurationSettingCommand : IRequest<UpdateConfigurationSettingResponse>,ICacheRemoverRequest, IRabbitMQService
    {
        public string CacheKey => "GetListConfigurationSettingQuery";

        public bool BypassCache { get; }
        public required ConfigurationSettingDto configurationSettingDto { get; set; }
        public ObjectId Id { get; set; }

        public string quequeName => "UpdateConfigurationSettingCommand";

        public class UpdateBrandCommandHandler : IRequestHandler<UpdateConfigurationSettingCommand, UpdateConfigurationSettingResponse>
        {
            private readonly ConfigurationService _configurationService;

            public UpdateBrandCommandHandler(ConfigurationService configurationService)
            {
                _configurationService = configurationService;
            }

            public async Task<UpdateConfigurationSettingResponse> Handle(UpdateConfigurationSettingCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    await _configurationService.UpdateAsync(request.Id, new ConfigurationSetting()
                    {
                        Id = request.Id,
                        ApplicationName = request.configurationSettingDto.ApplicationName,
                        IsActive = request.configurationSettingDto.IsActive,
                        Name = request.configurationSettingDto.Name,
                        Type = request.configurationSettingDto.Type,
                        Value = request.configurationSettingDto.Value
                    });
                    return new UpdateConfigurationSettingResponse() { IsSuccess = true };
                }
                catch (Exception ex)
                {
                    return new UpdateConfigurationSettingResponse() { IsSuccess = false };
                }
               
            }
        }
    }
}
