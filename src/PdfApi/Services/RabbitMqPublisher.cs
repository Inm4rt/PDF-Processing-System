using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PdfApi.Services
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _queueName = configuration["RabbitMQ:QueueName"] ?? "pdf_queue";
            _channel.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
        }

        public void PublishDocumentProcessing(Guid documentId)
        {
            var message = new DocumentProcessingMessage { DocumentId = documentId };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            _channel.BasicPublishAsync(exchange: "", routingKey: _queueName, body: body);
            _logger.LogInformation("Опубликовано сообщение для документа {DocumentId}", documentId);
        }

        public void Dispose()
        {
            _channel?.CloseAsync();
            _connection?.Dispose();
        }
    }
}
