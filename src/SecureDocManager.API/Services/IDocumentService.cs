using SecureDocManager.API.Models;

namespace SecureDocManager.API.Services
{
    public interface IDocumentService
    {
        Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string userId, string userName, string departmentId);
        Task<string> GenerateDownloadUrlAsync(string documentId, string userRole);
        Task<string> GenerateUploadUrlAsync(string fileName, string departmentId);
        Task<bool> DeleteDocumentAsync(int documentId);
        Task<Document?> GetDocumentByIdAsync(int documentId);
        Task<IEnumerable<Document>> GetDocumentsByDepartmentAsync(string departmentId, string userRole);
        Task<byte[]> SignDocumentAsync(byte[] document, string certificateName);
        Task<string> GetUserRoleAsync(string userId);
        Task<byte[]> DownloadDocumentAsync(int documentId);
        Task<IEnumerable<Document>> GetAllDocumentsAsync(string userRole);
        Task<Document> UpdateDocumentAsync(Document document);
    }
}
