using Microsoft.AspNetCore.Mvc;

namespace SecureDocManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "API está funcionando!", timestamp = DateTime.UtcNow });
        }

        [HttpGet("auth-info")]
        public IActionResult GetAuthInfo()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            
            return Ok(new 
            { 
                isAuthenticated,
                userName = User.Identity?.Name,
                claims
            });
        }
    }
} 