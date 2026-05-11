using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;

namespace PdfWorker.Services
{
    public class PdfTextExtractor : IPdfTextExtractor
    {
        private readonly ILogger<PdfTextExtractor> _logger;

        public PdfTextExtractor(ILogger<PdfTextExtractor> logger)
        {
            _logger = logger;
        }

        public string ExtractText(byte[] pdfData)
        {
            try
            {
                using var stream = new MemoryStream(pdfData);
                using var document = PdfDocument.Open(stream);
                var text = new StringBuilder();

                foreach (Page page in document.GetPages())
                {
                    text.Append(page.Text);
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка извлечения текста из PDF");
                throw;
            }
        }
    }
}
