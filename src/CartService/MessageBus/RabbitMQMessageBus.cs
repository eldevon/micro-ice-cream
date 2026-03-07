using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace CartService.MessageBus
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
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Declare exchanges and queues
                _channel.ExchangeDeclare("cart.events", ExchangeType.Topic, durable: true);
                
                _logger.LogInformation("Connected to RabbitMQ successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
        }

        public async Task PublishAsync<T>(string queue, T message)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Declare queue
                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    // Bind queue to exchange
                    _channel.QueueBind(queue, "cart.events", queue);

                    var json = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    _channel.BasicPublish(
                        exchange: "cart.events",
                        routingKey: queue,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Published message to queue {Queue}: {MessageType}", queue, typeof(T).Name);
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
                    // Declare queue
                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    // Bind queue to exchange
                    _channel.QueueBind(queue, "cart.events", queue);

                    var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(_channel);
                    
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
                                _logger.LogDebug("Processed message from queue {Queue}", queue);
                            }

                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message from queue {Queue}", queue);
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    };

                    _channel.BasicConsume(
                        queue: queue,
                        autoAck: false,
                        consumer: consumer);

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
            try
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
                
                _logger.LogInformation("RabbitMQ connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing RabbitMQ connection");
            }
        }
    }
}