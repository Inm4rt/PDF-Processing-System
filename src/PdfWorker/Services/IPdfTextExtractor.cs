namespace PdfWorker.Services
{
    public interface IPdfTextExtractor
    {
        string ExtractText(byte[] pdfData);
    }
}
