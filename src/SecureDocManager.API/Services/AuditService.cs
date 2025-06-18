using Microsoft.EntityFrameworkCore;
using SecureDocManager.API.Data;
using SecureDocManager.API.Models;

namespace SecureDocManager.API.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAccessAsync(string userId, string userName, int? documentId, string action, string? ipAddress = null, string? details = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    DocumentId = documentId,
                    Action = action,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Details = details
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit log created: {Action} by user {UserId} on document {DocumentId}", 
                    action, userId, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit log for action {Action} by user {UserId}", action, userId);
                // Não propagar a exceção para não afetar a operação principal
            }
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? documentId = null, string? userId = null, 
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (documentId.HasValue)
                query = query.Where(a => a.DocumentId == documentId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(1000) // Limitar a 1000 registros
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(string userId, int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            return await _context.AuditLogs
                .Where(a => a.UserId == userId && a.Timestamp >= startDate)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetDocumentHistoryAsync(int documentId)
        {
            return await _context.AuditLogs
                .Where(a => a.DocumentId == documentId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
    }
} 