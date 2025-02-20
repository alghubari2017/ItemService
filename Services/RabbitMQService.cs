using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ItemService.Services;

public class RabbitMQService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "item_exchange";
    private const int MaxRetries = 5;

    public RabbitMQService(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"],
            UserName = configuration["RabbitMQ:Username"],
            Password = configuration["RabbitMQ:Password"]
        };

        // Add retry logic
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);
                break;
            }
            catch
            {
                if (i == MaxRetries - 1) throw;
                Thread.Sleep(2000); // Wait 2 seconds before retrying
            }
        }
    }

    public void PublishItemEvent(string routingKey, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: null,
            body: body);
    }
}