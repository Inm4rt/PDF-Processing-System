namespace PdfApi.Services
{
    public interface IRabbitMqPublisher
    {
        void PublishDocumentProcessing(Guid documentId);
    }
}
