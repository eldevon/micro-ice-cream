using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CheckoutService.MessageBus
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
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Declare exchanges
                _channel.ExchangeDeclare("checkout.events", ExchangeType.Topic, durable: true);
                _channel.ExchangeDeclare("order.events", ExchangeType.Topic, durable: true);
                _channel.ExchangeDeclare("payment.events", ExchangeType.Topic, durable: true);
                
                _logger.LogInformation("Connected to RabbitMQ successfully for CheckoutService");
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

                    // Determine exchange based on queue name
                    string exchange = queue.StartsWith("order") ? "order.events" : 
                                     queue.StartsWith("payment") ? "payment.events" : 
                                     "checkout.events";

                    // Bind queue to exchange
                    _channel.QueueBind(queue, exchange, queue);

                    var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.MessageId = Guid.NewGuid().ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    _channel.BasicPublish(
                        exchange: exchange,
                        routingKey: queue,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Published message to queue {Queue} on exchange {Exchange}: {MessageType}", 
                        queue, exchange, typeof(T).Name);
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

                    // Determine exchange based on queue name
                    string exchange = queue.StartsWith("order") ? "order.events" : 
                                     queue.StartsWith("payment") ? "payment.events" : 
                                     "checkout.events";

                    // Bind queue to exchange
                    _channel.QueueBind(queue, exchange, queue);

                    var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(_channel);
                    
                    consumer.Received += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var json = Encoding.UTF8.GetString(body);
                            var message = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (message != null)
                            {
                                _logger.LogDebug("Processing message from queue {Queue}", queue);
                                await handler(message);
                                _logger.LogDebug("Successfully processed message from queue {Queue}", queue);
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

                    _logger.LogInformation("Subscribed to queue {Queue} on exchange {Exchange}", queue, exchange);
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
                
                _logger.LogInformation("RabbitMQ connection closed for CheckoutService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing RabbitMQ connection");
            }
        }
    }
}