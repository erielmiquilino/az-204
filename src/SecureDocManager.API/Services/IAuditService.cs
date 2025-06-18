using SecureDocManager.API.Models;

namespace SecureDocManager.API.Services
{
    public interface IAuditService
    {
        Task LogAccessAsync(string userId, string userName, int? documentId, string action, string? ipAddress = null, string? details = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? documentId = null, string? userId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<AuditLog>> GetUserActivityAsync(string userId, int days = 30);
        Task<IEnumerable<AuditLog>> GetDocumentHistoryAsync(int documentId);
    }
} 