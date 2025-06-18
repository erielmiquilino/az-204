using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using SecureDocManager.API.Data;
using SecureDocManager.API.Models;
using SecureDocManager.API.Services;
using System.Security.Claims;

namespace SecureDocManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGraphService _graphService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context, 
            IGraphService graphService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _graphService = graphService;
            _logger = logger;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.GetObjectId() ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("ID do usuário não encontrado");
                }

                // Buscar usuário no banco local
                var user = await _context.Users.FindAsync(userId);
                
                // Se não existir, buscar no Graph e criar
                if (user == null)
                {
                    var graphUser = await _graphService.GetUserProfileAsync(userId);
                    if (graphUser == null)
                    {
                        return NotFound("Usuário não encontrado no Azure AD");
                    }

                    user = new User
                    {
                        Id = userId,
                        DisplayName = graphUser.DisplayName ?? "Unknown",
                        Email = graphUser.Mail ?? graphUser.UserPrincipalName ?? "unknown@email.com",
                        Department = graphUser.Department,
                        JobTitle = graphUser.JobTitle,
                        Role = GetUserRole(),
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Atualizar último login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.Department,
                    user.JobTitle,
                    user.Role,
                    user.CreatedAt,
                    user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações do usuário atual");
                return StatusCode(500, "Erro ao processar requisição");
            }
        }

        [HttpGet("roles")]
        public IActionResult GetUserRoles()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            
            // Se não houver roles no token, determinar baseado em outras claims
            if (!roles.Any())
            {
                roles.Add(GetUserRole());
            }

            return Ok(new { roles });
        }

        [HttpGet("permissions")]
        public IActionResult GetUserPermissions()
        {
            var role = GetUserRole();
            
            var permissions = role switch
            {
                "Admin" => new[] 
                { 
                    "documents.read", 
                    "documents.write", 
                    "documents.delete", 
                    "documents.sign",
                    "users.read", 
                    "users.write",
                    "audit.read"
                },
                "Manager" => new[] 
                { 
                    "documents.read", 
                    "documents.write", 
                    "documents.sign",
                    "users.read",
                    "audit.read"
                },
                "Employee" => new[] 
                { 
                    "documents.read", 
                    "documents.write"
                },
                _ => Array.Empty<string>()
            };

            return Ok(new { permissions });
        }

        private string GetUserRole()
        {
            // Verificar claims de role
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(roleClaim))
            {
                return roleClaim;
            }

            // Verificar grupos do Azure AD (GUIDs dos grupos)
            var groups = User.FindAll("groups").Select(c => c.Value).ToList();
            
            // TODO: Mapear GUIDs de grupos para roles
            // Por enquanto, retornar Employee como padrão
            return "Employee";
        }
    }
}
