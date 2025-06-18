namespace SecureDocManager.API.Services
{
    public interface IDocumentSigningService
    {
        Task<byte[]> SignDocumentAsync(byte[] document, string certificateName);
        Task<bool> VerifySignatureAsync(byte[] document, byte[] signature, string certificateName);
        Task<string> CreateSignedDocumentUrlAsync(int documentId, string userId);
        Task<DocumentSignature> GetDocumentSignatureAsync(int documentId);
    }

    public class DocumentSignature
    {
        public int DocumentId { get; set; }
        public byte[] Signature { get; set; } = Array.Empty<byte>();
        public string SignedBy { get; set; } = string.Empty;
        public DateTime SignedAt { get; set; }
        public string CertificateThumbprint { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
} 