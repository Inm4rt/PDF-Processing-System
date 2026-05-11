using Microsoft.AspNetCore.Mvc;
using PdfApi.Data;
using PdfApi.DTOs;
using PdfApi.Models;
using PdfApi.Services;

namespace PdfApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRabbitMqPublisher _publisher;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(AppDbContext context, IRabbitMqPublisher publisher, ILogger<DocumentsController> logger)
        {
            _context = context;
            _publisher = publisher;
            _logger = logger;
        }

        // POST api/documents - загрузка PDF
        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            // Валидация
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран.");

            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                return BadRequest("Разрешены только PDF файлы.");

            if (file.ContentType != "application/pdf")
                return BadRequest("Неверный MIME тип файла. Ожидается application/pdf.");

            // Сохраняем в БД
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Status = DocumentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            document.FileData = memoryStream.ToArray();

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Отправляем сообщение в очередь
            _publisher.PublishDocumentProcessing(document.Id);

            _logger.LogInformation("Документ {DocumentId} загружен и поставлен в очередь", document.Id);
            return Ok(new { document.Id, document.FileName, document.Status });
        }

        // GET api/documents - список документов (без текста)
        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            var documents = _context.Documents
                .Select(d => new DocumentResponse
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    Status = d.Status,
                    CreatedAt = d.CreatedAt
                })
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            return Ok(documents);
        }

        // GET api/documents/{id}/text - получить текст документа
        [HttpGet("{id}/text")]
        public async Task<IActionResult> GetDocumentText(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound("Документ не найден.");

            if (document.Status == DocumentStatus.Pending || document.Status == DocumentStatus.Processing)
                return Conflict("Документ еще обрабатывается. Попробуйте позже.");

            if (document.Status == DocumentStatus.Failed)
                return BadRequest($"Ошибка обработки: {document.ErrorMessage}");

            var response = new DocumentTextResponse
            {
                Id = document.Id,
                FileName = document.FileName,
                Status = document.Status,
                TextContent = document.TextContent,
                ErrorMessage = null
            };

            return Ok(response);
        }
    }
}
