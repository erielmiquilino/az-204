using SecureDocManager.API.Models;

namespace SecureDocManager.API.Services
{
    public interface ICosmosService
    {
        Task<CosmosDocument> CreateDocumentAsync(CosmosDocument document);
        Task<CosmosDocument?> GetDocumentAsync(string id, string departmentId);
        Task<IEnumerable<CosmosDocument>> GetDocumentsByDepartmentAsync(string departmentId, string userRole);
        Task<CosmosDocument> UpdateDocumentAsync(CosmosDocument document);
        Task DeleteDocumentAsync(string id, string departmentId);
        Task AddAccessHistoryAsync(string documentId, string departmentId, AccessHistoryEntry entry);
        Task<IEnumerable<CosmosDocument>> SearchDocumentsAsync(string searchTerm, string departmentId, string userRole);
    }
}
