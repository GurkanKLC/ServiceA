using ConfigurationWebPage.Application.Pipelines.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ConfigurationWebPage.Application.Pipelines.MessageBroker;

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
