using ConfigurationWebPage.Application.Pipelines.Caching;
using ConfigurationWebPage.Application.Pipelines.MessageBroker;
using ConfigurationWebPage.Services;
using MediatR;
using MongoDB.Bson;

namespace ConfigurationWebPage.Application.Features.ConfigurationSettings.Commands.Delete
{
    public class DeleteConfigurationSettingCommand : IRequest<DeleteConfigurationSettingResponse>,ICacheRemoverRequest, IRabbitMQService
    {
        public string CacheKey => "GetListConfigurationSettingQuery";

        public bool BypassCache { get; }
        public required ObjectId Id { get; set; }

        public string quequeName => "DeleteConfigurationSettingCommand";

        public class DeleteConfigurationSettingCommandHandler : IRequestHandler<DeleteConfigurationSettingCommand, DeleteConfigurationSettingResponse>
        {

            private readonly ConfigurationService _configurationService;

            public DeleteConfigurationSettingCommandHandler(ConfigurationService configurationService)
            {
                _configurationService = configurationService;
            }

            public async Task<DeleteConfigurationSettingResponse> Handle(DeleteConfigurationSettingCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    await _configurationService.RemoveAsync(request.Id);
                    return new DeleteConfigurationSettingResponse() { IsSuccess = true };

                }
                catch (Exception ex)
                {
                    return new DeleteConfigurationSettingResponse() { IsSuccess = false };

                }
            }
        }
    }
}
