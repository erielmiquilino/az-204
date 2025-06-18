using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using SecureDocManager.API.Data;
using SecureDocManager.API.Models;
using SecureDocManager.API.Models.DTOs;
using SecureDocManager.API.Services;
using System.Security.Claims;

namespace SecureDocManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ICosmosService _cosmosService;
        private readonly IGraphService _graphService;
        private readonly IAuditService _auditService;
        private readonly IDocumentSigningService _documentSigningService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            ICosmosService cosmosService,
            IGraphService graphService,
            IAuditService auditService,
            IDocumentSigningService documentSigningService,
            ApplicationDbContext context,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _cosmosService = cosmosService;
            _graphService = graphService;
            _auditService = auditService;
            _documentSigningService = documentSigningService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto)
        {
            try
            {
                var userId = User.GetObjectId() ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Validar tipo de arquivo
                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".pptx" };
                var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Tipo de arquivo não permitido");
                }

                // Validar tamanho do arquivo (max 10MB)
                if (dto.File.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("Arquivo muito grande. Tamanho máximo: 10MB");
                }

                using var stream = dto.File.OpenReadStream();
                var document = await _documentService.UploadDocumentAsync(
                    stream, 
                    dto.File.FileName, 
                    userId, 
                    dto.DepartmentId);

                // Atualizar com informações adicionais
                document.Description = dto.Description;
                document.AccessLevel = dto.AccessLevel;
                document.Tags = dto.Tags;
                await _context.SaveChangesAsync();

                // Registrar no log de auditoria
                await _auditService.LogAccessAsync(userId, User.Identity?.Name ?? "Unknown", document.Id, "DocumentUploaded", HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { documentId = document.Id, message = "Documento enviado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do documento");
                return StatusCode(500, "Erro ao processar o upload");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                var userRole = GetUserRole();
                var documents = await _documentService.GetAllDocumentsAsync(userRole);
                
                var response = documents.Select(d => new DocumentResponseDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileExtension = d.FileExtension,
                    FileSizeInBytes = d.FileSizeInBytes,
                    DepartmentId = d.DepartmentId,
                    UploadedByUserName = d.UploadedByUserName,
                    UploadedAt = d.UploadedAt,
                    Description = d.Description,
                    AccessLevel = d.AccessLevel,
                    IsSigned = d.IsSigned,
                    Tags = string.IsNullOrEmpty(d.Tags)
                        ? new List<string>()
                        : d.Tags.Split(',').Select(t => t.Trim()).ToList()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter todos os documentos");
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            try
            {
                var userId = User.GetObjectId();
                var userRole = GetUserRole();

                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound();
                }

                // Verificar permissões
                if (!await HasAccessToDocumentAsync(userId, document, userRole))
                {
                    return Forbid();
                }

                // Gerar URL de download
                var downloadUrl = await _documentService.GenerateDownloadUrlAsync(id.ToString(), userRole);

                var response = new DocumentResponseDto
                {
                    Id = document.Id,
                    FileName = document.FileName,
                    FileExtension = document.FileExtension,
                    FileSizeInBytes = document.FileSizeInBytes,
                    DepartmentId = document.DepartmentId,
                    UploadedByUserName = document.UploadedByUserName,
                    UploadedAt = document.UploadedAt,
                    Description = document.Description,
                    AccessLevel = document.AccessLevel,
                    IsSigned = document.IsSigned,
                    DownloadUrl = downloadUrl,
                    Tags = string.IsNullOrEmpty(document.Tags) 
                        ? new List<string>() 
                        : document.Tags.Split(',').Select(t => t.Trim()).ToList()
                };

                // Registrar acesso no Cosmos DB
                await _cosmosService.AddAccessHistoryAsync(
                    document.Id.ToString(),
                    document.DepartmentId,
                    new AccessHistoryEntry
                    {
                        UserId = userId ?? "",
                        UserName = User.Identity?.Name ?? "Unknown",
                        Action = "View",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                // Registrar no log de auditoria
                await _auditService.LogAccessAsync(
                    userId ?? "", 
                    User.Identity?.Name ?? "Unknown", 
                    document.Id, 
                    "DocumentViewed", 
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter documento {DocumentId}", id);
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetDocumentsByDepartment(string departmentId)
        {
            try
            {
                var userRole = GetUserRole();
                var documents = await _documentService.GetDocumentsByDepartmentAsync(departmentId, userRole);

                var response = documents.Select(d => new DocumentResponseDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileExtension = d.FileExtension,
                    FileSizeInBytes = d.FileSizeInBytes,
                    DepartmentId = d.DepartmentId,
                    UploadedByUserName = d.UploadedByUserName,
                    UploadedAt = d.UploadedAt,
                    Description = d.Description,
                    AccessLevel = d.AccessLevel,
                    IsSigned = d.IsSigned,
                    Tags = string.IsNullOrEmpty(d.Tags)
                        ? new List<string>()
                        : d.Tags.Split(',').Select(t => t.Trim()).ToList()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter documentos do departamento {DepartmentId}", departmentId);
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpPost("sign")]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> SignDocument([FromBody] DocumentSignDto dto)
        {
            try
            {
                var userId = User.GetObjectId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Criar URL de documento assinado
                var signedDocumentUrl = await _documentSigningService.CreateSignedDocumentUrlAsync(dto.DocumentId, userId);

                // Atualizar documento como assinado
                var document = await _documentService.GetDocumentByIdAsync(dto.DocumentId);
                if (document == null)
                {
                    return NotFound();
                }

                document.IsSigned = true;
                document.SignedAt = DateTime.UtcNow;
                document.SignedByUserId = userId;
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Documento assinado com sucesso",
                    signedDocumentUrl = signedDocumentUrl
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acesso negado ao assinar documento");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argumento inválido ao assinar documento");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao assinar documento");
                return StatusCode(500, "Erro ao processar assinatura");
            }
        }

        [HttpPost("{id}/verify-signature")]
        public async Task<IActionResult> VerifySignature(int id)
        {
            try
            {
                var signature = await _documentSigningService.GetDocumentSignatureAsync(id);
                
                // Registrar verificação no log
                var userId = User.GetObjectId() ?? "";
                await _auditService.LogAccessAsync(
                    userId, 
                    User.Identity?.Name ?? "Unknown", 
                    id, 
                    "SignatureVerified", 
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                return Ok(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar assinatura do documento {DocumentId}", id);
                return StatusCode(500, "Erro ao verificar assinatura");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                var userId = User.GetObjectId();
                var success = await _documentService.DeleteDocumentAsync(id);

                if (!success)
                {
                    return NotFound();
                }

                await _auditService.LogAccessAsync(userId ?? "", User.Identity?.Name ?? "Unknown", id, "DocumentDeleted", HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new { message = "Documento deletado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar documento {DocumentId}", id);
                return StatusCode(500, "Erro ao processar exclusão");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] string searchTerm, [FromQuery] string departmentId)
        {
            try
            {
                var userRole = GetUserRole();
                var documents = await _cosmosService.SearchDocumentsAsync(searchTerm, departmentId, userRole);

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao pesquisar documentos");
                return StatusCode(500, "Erro ao processar pesquisa");
            }
        }

        private async Task<bool> HasAccessToDocumentAsync(string? userId, Document document, string userRole)
        {
            // Admin tem acesso total
            if (userRole == "Admin")
                return true;

            // Verificar se o usuário está no mesmo departamento
            var userDepartment = await _graphService.GetUserDepartmentAsync(userId ?? "");
            if (userDepartment != document.DepartmentId)
                return false;

            // Verificar nível de acesso
            var userAccessLevel = GetAccessLevel(userRole);
            return userAccessLevel >= document.AccessLevel;
        }

        private string GetUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim ?? "Employee";
        }

        private int GetAccessLevel(string role)
        {
            return role switch
            {
                "Admin" => 3,
                "Manager" => 2,
                "Employee" => 1,
                _ => 0
            };
        }


    }
}
