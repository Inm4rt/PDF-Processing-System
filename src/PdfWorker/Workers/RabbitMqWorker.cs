using Microsoft.EntityFrameworkCore.Metadata;
using PdfWorker.Data;
using PdfWorker.Models;
using PdfWorker.Services;
using PdfWorker.Workers.Message;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PdfWorker.Workers
{
    public class RabbitMqWorker : BackgroundService
    {
        private readonly ILogger<RabbitMqWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IChannel _channel;
        private readonly string _queueName;

        public RabbitMqWorker(ILogger<RabbitMqWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _queueName = configuration["RabbitMQ:QueueName"] ?? "pdf_queue";
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest"
            };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
            _channel.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.BasicQosAsync(0, 1, false);

            _logger.LogInformation("Worker подписан на очередь {QueueName}", _queueName);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                DocumentProcessingMessage? message = null;

                try
                {
                    message = JsonSerializer.Deserialize<DocumentProcessingMessage>(messageJson);
                    if (message == null || message.DocumentId == Guid.Empty)
                    {
                        _logger.LogWarning("Получено невалидное сообщение");
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    _logger.LogInformation("Получено сообщение для документа {DocumentId}", message.DocumentId);
                    await ProcessDocument(message.DocumentId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки сообщения {MessageJson}", messageJson);
                    // В случае ошибки – отказ от подтверждения (не рекью, чтобы не зацикливать)
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessDocument(Guid documentId)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var textExtractor = scope.ServiceProvider.GetRequiredService<IPdfTextExtractor>();

            var document = await dbContext.Documents.FindAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("Документ {DocumentId} не найден в БД", documentId);
                return;
            }

            if (document.Status != DocumentStatus.Pending)
            {
                _logger.LogInformation("Документ {DocumentId} уже имеет статус {Status}, пропускаем", documentId, document.Status);
                return;
            }

            // Обновляем статус на "Processing"
            document.Status = DocumentStatus.Processing;
            document.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            try
            {
                _logger.LogInformation("Извлечение текста из документа {DocumentId}", documentId);
                var extractedText = textExtractor.ExtractText(document.FileData);

                document.TextContent = string.IsNullOrWhiteSpace(extractedText) ? "[Нет текста]" : extractedText;
                document.Status = DocumentStatus.Completed;
                document.ErrorMessage = null;
                _logger.LogInformation("Документ {DocumentId} успешно обработан. Длина текста: {Length}", documentId, extractedText.Length);
            }
            catch (Exception ex)
            {
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = $"Ошибка извлечения текста: {ex.Message}";
                _logger.LogError(ex, "Ошибка обработки документа {DocumentId}", documentId);
            }
            finally
            {
                document.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
