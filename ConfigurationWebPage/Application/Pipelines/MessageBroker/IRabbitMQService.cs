namespace ConfigurationWebPage.Application.Pipelines.MessageBroker
{
    public interface IRabbitMQService
    {
        string quequeName { get; }
    }
}
