using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Identity;
using System.Security.Cryptography;

namespace SecureDocManager.API.Services
{
    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient? _secretClient;
        private readonly CertificateClient? _certificateClient;
        private readonly KeyClient? _keyClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(
            SecretClient? secretClient,
            IConfiguration configuration,
            ILogger<KeyVaultService> logger)
        {
            _secretClient = secretClient!;
            _configuration = configuration;
            _logger = logger;
            
            var keyVaultUrl = configuration["KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                var credential = new DefaultAzureCredential();
                _certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
                _keyClient = new KeyClient(new Uri(keyVaultUrl), credential);
            }
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            if (_secretClient == null)
            {
                _logger.LogWarning("SecretClient não configurado. Usando valor local para {SecretName}", secretName);
                return GetLocalFallbackValue(secretName);
            }

            try
            {
                _logger.LogDebug("Buscando secret {SecretName} no Key Vault", secretName);
                var secret = await _secretClient.GetSecretAsync(secretName);
                _logger.LogInformation("✓ Secret {SecretName} obtido com sucesso do Key Vault", secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao obter secret {SecretName} do Key Vault", secretName);
                
                // Fallback para configuração local em desenvolvimento
                var localValue = GetLocalFallbackValue(secretName);
                if (!string.IsNullOrEmpty(localValue))
                {
                    _logger.LogWarning("⚠️ Usando valor local para {SecretName}", secretName);
                    return localValue;
                }
                
                throw new InvalidOperationException($"Secret {secretName} não encontrado no Key Vault e não há fallback local configurado.", ex);
            }
        }

        private string GetLocalFallbackValue(string secretName)
        {
            // Mapear nomes dos secrets para configurações locais
            var localKey = secretName switch
            {
                "DatabaseConnectionString" => "DatabaseConnectionString",
                "CosmosDBConnectionString" => "CosmosDBConnectionString", 
                "StorageConnectionString" => "StorageConnectionString",
                _ => secretName
            };
            
            return _configuration[localKey] ?? string.Empty;
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
        
        public async Task<bool> CreateUserCertificateAsync(string userId, string userEmail, string userName)
        {
            try
            {
                if (_certificateClient == null)
                {
                    throw new InvalidOperationException("Certificate client não está configurado");
                }

                var certificateName = GetCertificateName(userId);
                
                // Verificar se já existe
                try
                {
                    var existing = await _certificateClient.GetCertificateAsync(certificateName);
                    if (existing.Value != null)
                    {
                        _logger.LogInformation("Certificado já existe para o usuário {UserId}", userId);
                        return true;
                    }
                }
                catch
                {
                    // Certificado não existe, vamos criar
                }

                // Configurar política do certificado
                var policy = new CertificatePolicy("Self", $"CN={userName}, E={userEmail}")
                {
                    ContentType = CertificateContentType.Pkcs12,
                    ValidityInMonths = 12,
                    Exportable = false,
                    KeyType = CertificateKeyType.Rsa,
                    KeySize = 2048,
                    ReuseKey = false,
                    KeyUsage =
                    {
                        CertificateKeyUsage.DigitalSignature,
                        CertificateKeyUsage.NonRepudiation
                    }
                };

                // Criar certificado
                var operation = await _certificateClient.StartCreateCertificateAsync(certificateName, policy);
                await operation.WaitForCompletionAsync();

                _logger.LogInformation("Certificado criado com sucesso para o usuário {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar certificado para o usuário {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UserCertificateExistsAsync(string userId)
        {
            try
            {
                if (_certificateClient == null)
                {
                    return false;
                }

                var certificateName = GetCertificateName(userId);
                var certificate = await _certificateClient.GetCertificateAsync(certificateName);
                
                return certificate.Value != null && 
                       certificate.Value.Properties.Enabled == true &&
                       certificate.Value.Properties.ExpiresOn > DateTimeOffset.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]> SignDataAsync(byte[] hash, string userId)
        {
            try
            {
                if (_keyClient == null)
                {
                    throw new InvalidOperationException("Key client não está configurado");
                }

                var keyName = GetCertificateName(userId);
                var key = await _keyClient.GetKeyAsync(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());

                var signResult = await cryptoClient.SignAsync(SignatureAlgorithm.RS256, hash);
                
                _logger.LogInformation("Dados assinados com sucesso para o usuário {UserId}", userId);
                return signResult.Signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao assinar dados para o usuário {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, string userId)
        {
            try
            {
                if (_keyClient == null)
                {
                    throw new InvalidOperationException("Key client não está configurado");
                }

                var keyName = GetCertificateName(userId);
                var key = await _keyClient.GetKeyAsync(keyName);
                var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());

                var verifyResult = await cryptoClient.VerifyAsync(SignatureAlgorithm.RS256, data, signature);
                
                return verifyResult.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar assinatura do usuário {UserId}", userId);
                return false;
            }
        }

        public async Task<CertificateInfo> GetUserCertificateInfoAsync(string userId)
        {
            try
            {
                if (_certificateClient == null)
                {
                    throw new InvalidOperationException("Certificate client não está configurado");
                }

                var certificateName = GetCertificateName(userId);
                var certificate = await _certificateClient.GetCertificateAsync(certificateName);
                
                return new CertificateInfo
                {
                    Thumbprint = BitConverter.ToString(certificate.Value.Properties.X509Thumbprint).Replace("-", ""),
                    ValidFrom = certificate.Value.Properties.NotBefore?.DateTime ?? DateTime.MinValue,
                    ValidTo = certificate.Value.Properties.ExpiresOn?.DateTime ?? DateTime.MinValue,
                    Subject = $"CN={userId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações do certificado do usuário {UserId}", userId);
                throw;
            }
        }

        private string GetCertificateName(string userId)
        {
            // Normalizar o userId para um nome válido de certificado
            return $"user-cert-{userId.Replace("@", "-").Replace(".", "-").ToLower()}";
        }
    }
}
