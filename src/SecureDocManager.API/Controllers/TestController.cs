using Microsoft.AspNetCore.Mvc;
using SecureDocManager.API.Services;

namespace SecureDocManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly IDocumentService _documentService;
        private readonly IKeyVaultService _keyVaultService;

        public TestController(
            ILogger<TestController> logger,
            IDocumentService documentService,
            IKeyVaultService keyVaultService)
        {
            _logger = logger;
            _documentService = documentService;
            _keyVaultService = keyVaultService;
        }

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

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        [HttpGet("storage-connection")]
        public async Task<IActionResult> TestStorageConnection()
        {
            try
            {
                var connectionString = await _keyVaultService.GetStorageConnectionStringAsync();
                var masked = MaskConnectionString(connectionString);
                
                return Ok(new { 
                    status = "success", 
                    message = "Connection string obtida com sucesso",
                    connectionString = masked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter connection string do storage");
                return BadRequest(new { 
                    status = "error", 
                    message = ex.Message 
                });
            }
        }

        [HttpPost("storage-upload")]
        public async Task<IActionResult> TestStorageUpload()
        {
            try
            {
                var testContent = "Este é um arquivo de teste criado em " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testContent));
                
                var document = await _documentService.UploadDocumentAsync(
                    stream, 
                    $"teste-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt", 
                    "test-user", 
                    "test-user-name",
                    "test-department"
                );
                
                return Ok(new { 
                    status = "success", 
                    message = "Upload realizado com sucesso",
                    documentId = document.Id,
                    fileName = document.FileName,
                    size = document.FileSizeInBytes,
                    url = document.BlobStorageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar upload no storage");
                return BadRequest(new { 
                    status = "error", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("network-diagnostics")]
        public async Task<IActionResult> NetworkDiagnostics()
        {
            var diagnostics = new List<object>();
            
            // Teste 1: Conectividade local do Azurite
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await httpClient.GetAsync("http://127.0.0.1:10000/devstoreaccount1");
                diagnostics.Add(new { 
                    test = "Azurite Local", 
                    status = "success", 
                    statusCode = response.StatusCode 
                });
            }
            catch (Exception ex)
            {
                diagnostics.Add(new { 
                    test = "Azurite Local", 
                    status = "failed", 
                    error = ex.Message 
                });
            }

            // Teste 2: Conectividade com Azure Storage público
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var response = await httpClient.GetAsync("https://azure.microsoft.com");
                diagnostics.Add(new { 
                    test = "Azure Connectivity", 
                    status = "success", 
                    statusCode = response.StatusCode 
                });
            }
            catch (Exception ex)
            {
                diagnostics.Add(new { 
                    test = "Azure Connectivity", 
                    status = "failed", 
                    error = ex.Message 
                });
            }

            return Ok(new { 
                timestamp = DateTime.UtcNow,
                diagnostics 
            });
        }

        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "EMPTY";
            
            if (connectionString.Contains("UseDevelopmentStorage=true"))
                return "UseDevelopmentStorage=true (Azurite)";
            
            // Mascarar chaves sensíveis
            var masked = connectionString;
            if (masked.Contains("AccountKey="))
            {
                var keyStart = masked.IndexOf("AccountKey=") + "AccountKey=".Length;
                var keyEnd = masked.IndexOf(";", keyStart);
                if (keyEnd == -1) keyEnd = masked.Length;
                
                var key = masked.Substring(keyStart, keyEnd - keyStart);
                if (key.Length > 8)
                {
                    masked = masked.Replace(key, key.Substring(0, 4) + "***" + key.Substring(key.Length - 4));
                }
            }
            
            return masked;
        }
    }
} 