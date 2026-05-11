namespace PdfApi.DTOs
{
    public class DocumentResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
