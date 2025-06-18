using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using SecureDocManager.API.Services;
using System.Security.Claims;

namespace SecureDocManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Obter logs de auditoria (apenas Admin e Manager)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int? documentId = null,
            [FromQuery] string? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var logs = await _auditService.GetAuditLogsAsync(documentId, userId, startDate, endDate);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter logs de auditoria");
                return StatusCode(500, "Erro ao obter logs de auditoria");
            }
        }

        /// <summary>
        /// Obter atividade do usuário atual
        /// </summary>
        [HttpGet("my-activity")]
        public async Task<IActionResult> GetMyActivity([FromQuery] int days = 30)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var activity = await _auditService.GetUserActivityAsync(userId, days);
                return Ok(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter atividade do usuário");
                return StatusCode(500, "Erro ao obter atividade do usuário");
            }
        }

        /// <summary>
        /// Obter histórico de um documento específico
        /// </summary>
        [HttpGet("document/{documentId}/history")]
        public async Task<IActionResult> GetDocumentHistory(int documentId)
        {
            try
            {
                var history = await _auditService.GetDocumentHistoryAsync(documentId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter histórico do documento {DocumentId}", documentId);
                return StatusCode(500, "Erro ao obter histórico do documento");
            }
        }
    }
} 