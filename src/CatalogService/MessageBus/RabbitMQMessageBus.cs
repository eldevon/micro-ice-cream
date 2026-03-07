using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CatalogService.MessageBus
{
    public class RabbitMQMessageBus : IMessageBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQMessageBus> _logger;

        public RabbitMQMessageBus(IConfiguration configuration, ILogger<RabbitMQMessageBus> logger)
        {
            _logger = logger;
            
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public async Task PublishAsync<T>(string queue, T message)
        {
            await Task.Run(() =>
            {
                try
                {
                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var json = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(json);

                    _channel.BasicPublish(
                        exchange: "",
                        routingKey: queue,
                        basicProperties: null,
                        body: body);

                    _logger.LogInformation("Published message to queue {Queue}: {Message}", queue, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing message to queue {Queue}", queue);
                    throw;
                }
            });
        }

        public async Task SubscribeAsync<T>(string queue, Func<T, Task> handler)
        {
            await Task.Run(() =>
            {
                try
                {
                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
                    
                    consumer.Received += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var json = Encoding.UTF8.GetString(body);
                            var message = JsonSerializer.Deserialize<T>(json);

                            if (message != null)
                            {
                                await handler(message);
                            }

                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message from queue {Queue}", queue);
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    };

                    _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);

                    _logger.LogInformation("Subscribed to queue {Queue}", queue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error subscribing to queue {Queue}", queue);
                    throw;
                }
            });
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}