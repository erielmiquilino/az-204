using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Certificates;
using Azure.Identity;

namespace SecureDocManager.API.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient _secretClient;
        private readonly CertificateClient? _certificateClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(
            SecretClient secretClient, 
            IConfiguration configuration,
            ILogger<KeyVaultService> logger)
        {
            _secretClient = secretClient;
            _configuration = configuration;
            _logger = logger;
            
            var keyVaultUrl = configuration["KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                _certificateClient = new CertificateClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            }
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                var secret = await _secretClient.GetSecretAsync(secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter secret {SecretName} do Key Vault", secretName);
                
                // Fallback para configuração local em desenvolvimento
                var localValue = _configuration[secretName];
                if (!string.IsNullOrEmpty(localValue))
                {
                    _logger.LogWarning("Usando valor local para {SecretName}", secretName);
                    return localValue;
                }
                
                throw;
            }
        }

        public async Task<string> GetConnectionStringAsync()
        {
            return await GetSecretAsync("DatabaseConnectionString");
        }

        public async Task<string> GetStorageConnectionStringAsync()
        {
            return await GetSecretAsync("StorageConnectionString");
        }

        public async Task<string> GetCosmosConnectionStringAsync()
        {
            return await GetSecretAsync("CosmosDBConnectionString");
        }

        public async Task<byte[]> GetCertificateAsync(string certificateName)
        {
            try
            {
                if (_certificateClient == null)
                {
                    throw new InvalidOperationException("Certificate client não está configurado");
                }

                var certificate = await _certificateClient.GetCertificateAsync(certificateName);
                return certificate.Value.Cer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter certificado {CertificateName} do Key Vault", certificateName);
                throw;
            }
        }
    }
}
