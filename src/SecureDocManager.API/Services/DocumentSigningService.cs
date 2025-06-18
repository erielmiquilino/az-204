using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace SecureDocManager.API.Services
{
    public class DocumentSigningService : IDocumentSigningService
    {
        private readonly IKeyVaultService _keyVaultService;
        private readonly IAuditService _auditService;
        private readonly IDocumentService _documentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentSigningService> _logger;
        private readonly KeyClient? _keyClient;

        public DocumentSigningService(
            IKeyVaultService keyVaultService,
            IAuditService auditService,
            IDocumentService documentService,
            IConfiguration configuration,
            ILogger<DocumentSigningService> logger)
        {
            _keyVaultService = keyVaultService;
            _auditService = auditService;
            _documentService = documentService;
            _configuration = configuration;
            _logger = logger;

            var keyVaultUrl = configuration["KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                _keyClient = new KeyClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            }
        }

        public async Task<byte[]> SignDocumentAsync(byte[] document, string certificateName)
        {
            try
            {
                if (_keyClient == null)
                {
                    throw new InvalidOperationException("Key client não está configurado");
                }

                // Obter a chave do certificado no Key Vault
                var keyVaultKey = await _keyClient.GetKeyAsync(certificateName);
                var cryptoClient = new CryptographyClient(keyVaultKey.Value.Id, new DefaultAzureCredential());

                // Calcular hash do documento
                using var sha256 = SHA256.Create();
                var documentHash = sha256.ComputeHash(document);

                // Assinar o hash usando a chave do Key Vault
                var signResult = await cryptoClient.SignAsync(SignatureAlgorithm.RS256, documentHash);

                _logger.LogInformation("Documento assinado com sucesso usando certificado {CertificateName}", certificateName);

                return signResult.Signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao assinar documento com certificado {CertificateName}", certificateName);
                throw;
            }
        }

        public async Task<bool> VerifySignatureAsync(byte[] document, byte[] signature, string certificateName)
        {
            try
            {
                var certificateBytes = await _keyVaultService.GetCertificateAsync(certificateName);
                using var certificate = X509CertificateLoader.LoadCertificate(certificateBytes);
                
                using var rsa = certificate.GetRSAPublicKey();
                if (rsa == null)
                {
                    throw new InvalidOperationException("Não foi possível obter a chave pública do certificado");
                }

                using var sha256 = SHA256.Create();
                var documentHash = sha256.ComputeHash(document);

                return rsa.VerifyData(documentHash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar assinatura do documento");
                return false;
            }
        }

        public async Task<string> CreateSignedDocumentUrlAsync(int documentId, string userId)
        {
            try
            {
                // Obter o documento
                var document = await _documentService.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    throw new ArgumentException($"Documento {documentId} não encontrado");
                }

                // Verificar permissões (apenas Admin e Manager podem assinar)
                var userRole = await _documentService.GetUserRoleAsync(userId);
                if (userRole != "Admin" && userRole != "Manager")
                {
                    throw new UnauthorizedAccessException("Usuário não tem permissão para assinar documentos");
                }

                // Baixar o conteúdo do documento
                var documentContent = await _documentService.DownloadDocumentAsync(documentId);

                // Assinar o documento
                var signature = await SignDocumentAsync(documentContent, "document-signing-cert");

                // Salvar a assinatura (em produção, isso seria salvo no banco de dados)
                // Por enquanto, vamos apenas registrar no audit log
                await _auditService.LogAccessAsync(
                    userId,
                    "Sistema",
                    documentId,
                    "DocumentSigned",
                    null,
                    $"Documento assinado digitalmente. Thumbprint: {Convert.ToBase64String(signature).Substring(0, 20)}..."
                );

                // Gerar URL temporária para download
                return await _documentService.GenerateDownloadUrlAsync(documentId.ToString(), userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar URL de documento assinado para documento {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<DocumentSignature> GetDocumentSignatureAsync(int documentId)
        {
            // Em produção, isso viria do banco de dados
            // Por enquanto, vamos simular
            await Task.Delay(100); // Simular operação assíncrona

            return new DocumentSignature
            {
                DocumentId = documentId,
                Signature = Array.Empty<byte>(),
                SignedBy = "Sistema",
                SignedAt = DateTime.UtcNow,
                CertificateThumbprint = "SIMULADO",
                IsValid = false
            };
        }
    }
} 