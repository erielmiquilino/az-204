using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using SecureDocManager.API.Data;
using SecureDocManager.API.Models;
using SecureDocManager.API.Services;

namespace SecureDocManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGraphService _graphService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            IGraphService graphService,
            ILogger<UsersController> logger)
        {
            _context = context;
            _graphService = graphService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => new
                    {
                        u.Id,
                        u.DisplayName,
                        u.Email,
                        u.Department,
                        u.JobTitle,
                        u.Role,
                        u.LastLoginAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter lista de usuários");
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("department/{department}")]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> GetUsersByDepartment(string department)
        {
            try
            {
                // Buscar usuários do Azure AD
                var graphUsers = await _graphService.GetUsersInDepartmentAsync(department);

                // Buscar usuários locais
                var localUsers = await _context.Users
                    .Where(u => u.Department == department && u.IsActive)
                    .ToListAsync();

                var result = graphUsers.Select(gu => new
                {
                    Id = gu.Id,
                    DisplayName = gu.DisplayName,
                    Email = gu.Mail,
                    Department = gu.Department,
                    JobTitle = gu.JobTitle,
                    IsInLocalDb = localUsers.Any(lu => lu.Id == gu.Id),
                    Role = localUsers.FirstOrDefault(lu => lu.Id == gu.Id)?.Role ?? "Employee"
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter usuários do departamento {Department}", department);
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.GetObjectId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found in token" });
                }

                // Primeiro tentar buscar do banco local
                var user = await _context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    // Se não existir, criar um novo usuário com dados básicos do token
                    user = new User
                    {
                        Id = userId,
                        DisplayName = User.Identity?.Name ?? "Unknown User",
                        Email = User.FindFirst("preferred_username")?.Value ?? 
                                User.FindFirst("email")?.Value ?? 
                                User.FindFirst("upn")?.Value ?? "",
                        Department = User.FindFirst("department")?.Value ?? "N/A",
                        JobTitle = User.FindFirst("jobTitle")?.Value ?? "N/A",
                        Role = "Employee", // Role padrão
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Atualizar último login
                    user.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.Department,
                    user.JobTitle,
                    user.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter perfil do usuário");
                return StatusCode(500, new { error = "Erro ao processar requisição", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var currentUserId = User.GetObjectId();
                var isAdmin = User.IsInRole("Admin");
                var isManager = User.IsInRole("Manager");

                // Usuários só podem ver seus próprios dados, exceto Admin e Manager
                if (id != currentUserId && !isAdmin && !isManager)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    // Tentar buscar no Graph
                    var graphUser = await _graphService.GetUserProfileAsync(id);
                    if (graphUser == null)
                    {
                        return NotFound();
                    }

                    return Ok(new
                    {
                        Id = graphUser.Id,
                        DisplayName = graphUser.DisplayName,
                        Email = graphUser.Mail,
                        Department = graphUser.Department,
                        JobTitle = graphUser.JobTitle,
                        IsInLocalDb = false
                    });
                }

                return Ok(new
                {
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.Department,
                    user.JobTitle,
                    user.Role,
                    user.CreatedAt,
                    user.LastLoginAt,
                    IsInLocalDb = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter usuário {UserId}", id);
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("{id}/photo")]
        public async Task<IActionResult> GetUserPhoto(string id)
        {
            try
            {
                var photo = await _graphService.GetUserPhotoAsync(id);
                if (photo == null)
                {
                    return NotFound();
                }

                return File(photo, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter foto do usuário {UserId}", id);
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpPut("{id}/role")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateRoleDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var validRoles = new[] { "Admin", "Manager", "Employee" };
                if (!validRoles.Contains(dto.Role))
                {
                    return BadRequest("Role inválido");
                }

                user.Role = dto.Role;
                await _context.SaveChangesAsync();

                // Registrar auditoria
                var auditLog = new AuditLog
                {
                    UserId = User.GetObjectId() ?? "",
                    UserName = User.Identity?.Name ?? "Unknown",
                    Action = $"UpdateRole: {id} -> {dto.Role}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Role atualizado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar role do usuário {UserId}", id);
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("audit-logs")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int? page = 1, [FromQuery] int? pageSize = 50)
        {
            try
            {
                var query = _context.AuditLogs.OrderByDescending(a => a.Timestamp);

                var totalItems = await query.CountAsync();
                var logs = await query
                    .Skip(((page ?? 1) - 1) * (pageSize ?? 50))
                    .Take(pageSize ?? 50)
                    .Select(a => new
                    {
                        a.Id,
                        a.UserId,
                        a.UserName,
                        a.DocumentId,
                        a.Action,
                        a.Timestamp,
                        a.IpAddress,
                        a.Details
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = logs,
                    totalItems,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalItems / (double)(pageSize ?? 50))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter logs de auditoria");
                return StatusCode(500, "Erro ao processar requisição");
            }
        }
    }

    public class UpdateRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }
}
