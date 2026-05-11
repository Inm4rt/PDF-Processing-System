using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdfWorker.Models
{
    [Table("Documents")]
    public class Document
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        [Required]
        public string Status { get; set; } = DocumentStatus.Pending;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? TextContent { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public static class DocumentStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
