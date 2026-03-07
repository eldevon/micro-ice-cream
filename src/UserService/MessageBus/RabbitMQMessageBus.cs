using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.MessageBus
{
    public class RabbitMQMessageBus : IMessageBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQMessageBus> _logger;
        private readonly string _exchangeName = "user.events";

        public RabbitMQMessageBus(IConfiguration configuration, ILogger<RabbitMQMessageBus> logger)
        {
            _logger = logger;
            
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                Port = configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672,
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            try
            {
                _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}...", factory.HostName, factory.Port);
                
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Declare exchanges for user events
                _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);
                _channel.ExchangeDeclare("auth.events", ExchangeType.Topic, durable: true);
                
                // Declare dead letter exchange for failed messages
                _channel.ExchangeDeclare("user.dlx", ExchangeType.Topic, durable: true);
                
                _logger.LogInformation("Successfully connected to RabbitMQ for UserService. Exchange: {Exchange}", _exchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", factory.HostName, factory.Port);
                throw;
            }
        }

        public async Task PublishAsync<T>(string queue, T message)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Declare queue with dead letter configuration
                    var args = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", "user.dlx" },
                        { "x-dead-letter-routing-key", queue + ".dead" },
                        { "x-max-priority", 10 },
                        { "x-message-ttl", 86400000 } // 24 hours in milliseconds
                    };

                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: args);

                    // Determine exchange based on queue name
                    string exchange = queue.StartsWith("auth") ? "auth.events" : _exchangeName;

                    // Bind queue to exchange
                    _channel.QueueBind(queue, exchange, queue);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };
                    
                    var json = JsonSerializer.Serialize(message, options);
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.MessageId = Guid.NewGuid().ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.Priority = 5; // Normal priority
                    properties.Headers = new Dictionary<string, object>
                    {
                        { "message-type", typeof(T).Name },
                        { "published-at", DateTime.UtcNow.ToString("o") },
                        { "service", "UserService" }
                    };

                    _channel.BasicPublish(
                        exchange: exchange,
                        routingKey: queue,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation("Published message to queue {Queue} on exchange {Exchange}: {MessageType} (ID: {MessageId})", 
                        queue, exchange, typeof(T).Name, properties.MessageId);
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
                    // Declare queue with dead letter configuration
                    var args = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", "user.dlx" },
                        { "x-dead-letter-routing-key", queue + ".dead" },
                        { "x-max-priority", 10 }
                    };

                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: args);

                    // Determine exchange based on queue name
                    string exchange = queue.StartsWith("auth") ? "auth.events" : _exchangeName;

                    // Bind queue to exchange
                    _channel.QueueBind(queue, exchange, queue);

                    // Set QoS for fair dispatch
                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                    var consumer = new RabbitMQ.Client.Events.AsyncEventingBasicConsumer(_channel);
                    
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var messageId = ea.BasicProperties?.MessageId ?? "unknown";
                        
                        try
                        {
                            _logger.LogDebug("Received message from queue {Queue} (ID: {MessageId})", queue, messageId);

                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            
                            var message = JsonSerializer.Deserialize<T>(json, options);

                            if (message != null)
                            {
                                _logger.LogInformation("Processing message from queue {Queue}: {MessageType} (ID: {MessageId})", 
                                    queue, typeof(T).Name, messageId);
                                
                                await handler(message);
                                
                                _logger.LogInformation("Successfully processed message from queue {Queue} (ID: {MessageId})", 
                                    queue, messageId);
                            }

                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message from queue {Queue} (ID: {MessageId})", queue, messageId);
                            
                            // Reject and requeue based on retry count
                            var retryCount = GetRetryCount(ea.BasicProperties);
                            if (retryCount < 3)
            {
                                // Increment retry count and requeue
                                var properties = _channel.CreateBasicProperties();
                                properties.Headers = ea.BasicProperties?.Headers ?? new Dictionary<string, object>();
                                properties.Headers["retry-count"] = retryCount + 1;
                                
                                _channel.BasicPublish(
                                    exchange: exchange,
                                    routingKey: queue,
                                    basicProperties: properties,
                                    body: body);
                                
                                _channel.BasicAck(ea.DeliveryTag, false);
                                
                                _logger.LogWarning("Requeued message for retry {RetryCount}/3 from queue {Queue} (ID: {MessageId})", 
                                    retryCount + 1, queue, messageId);
                            }
                            else
                            {
                                // Send to dead letter queue
                                _channel.BasicNack(ea.DeliveryTag, false, false);
                                _logger.LogError("Message moved to dead letter queue after 3 retries (ID: {MessageId})", messageId);
                            }
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

        private int GetRetryCount(IBasicProperties? properties)
        {
            if (properties?.Headers != null && properties.Headers.TryGetValue("retry-count", out var retryObj))
            {
                return Convert.ToInt32(retryObj);
            }
            return 0;
        }

        public void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
                
                _logger.LogInformation("RabbitMQ connection closed for UserService");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing RabbitMQ connection for UserService");
            }
        }
    }
}