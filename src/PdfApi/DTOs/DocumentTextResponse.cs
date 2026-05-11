namespace PdfApi.DTOs
{
    public class DocumentTextResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
