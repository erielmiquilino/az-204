using SecureDocManager.API.Models;

namespace SecureDocManager.API.Services
{
    public interface IDocumentSigningService
    {
        Task<DocumentSignature> SignDocumentAsync(int documentId, string userId, string userEmail, string userName);
        Task<bool> VerifySignatureAsync(int documentId, string signatureId);
        Task<IEnumerable<DocumentSignature>> GetDocumentSignaturesAsync(int documentId);
        Task<bool> EnsureUserCertificateAsync(string userId, string userEmail, string userName);
    }
} 