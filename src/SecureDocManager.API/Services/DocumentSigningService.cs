using System.Security.Cryptography;
using Azure.Storage.Blobs;
using SecureDocManager.API.Models;

namespace SecureDocManager.API.Services
{
    public class DocumentSigningService : IDocumentSigningService
    {
        private readonly IKeyVaultService _keyVaultService;
        private readonly ICosmosService _cosmosService;
        private readonly IDocumentService _documentService;
        private readonly IAuditService _auditService;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<DocumentSigningService> _logger;

        public DocumentSigningService(
            IKeyVaultService keyVaultService,
            ICosmosService cosmosService,
            IDocumentService documentService,
            IAuditService auditService,
            BlobServiceClient blobServiceClient,
            ILogger<DocumentSigningService> logger)
        {
            _keyVaultService = keyVaultService;
            _cosmosService = cosmosService;
            _documentService = documentService;
            _auditService = auditService;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        public async Task<bool> EnsureUserCertificateAsync(string userId, string userEmail, string userName)
        {
            try
            {
                // Verificar se o certificado já existe
                if (await _keyVaultService.UserCertificateExistsAsync(userId))
                {
                    _logger.LogInformation("Certificado já existe para o usuário {UserId}", userId);
                    return true;
                }

                // Criar novo certificado
                _logger.LogInformation("Criando novo certificado para o usuário {UserId}", userId);
                return await _keyVaultService.CreateUserCertificateAsync(userId, userEmail, userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao garantir certificado para o usuário {UserId}", userId);
                throw;
            }
        }

        public async Task<DocumentSignature> SignDocumentAsync(int documentId, string userId, string userEmail, string userName)
        {
            try
            {
                // Garantir que o usuário tem um certificado
                var certificateReady = await EnsureUserCertificateAsync(userId, userEmail, userName);
                if (!certificateReady)
                {
                    _logger.LogError("Não foi possível criar ou verificar o certificado do usuário {UserId}. Verifique as permissões no Key Vault.", userId);
                    throw new InvalidOperationException("Não foi possível criar ou verificar o certificado do usuário. Verifique as permissões no Key Vault.");
                }

                // Buscar o documento
                var document = await _documentService.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    throw new ArgumentException($"Documento {documentId} não encontrado");
                }

                // Baixar o arquivo do blob storage
                var containerClient = _blobServiceClient.GetBlobContainerClient("documents");
                
                var blobUri = new Uri(document.BlobStorageUrl);
                var pathAndQuery = blobUri.PathAndQuery;
                var blobName = pathAndQuery.Substring(pathAndQuery.IndexOf('/', 1) + 1);

                if (string.IsNullOrEmpty(blobName) || blobName == "/")
                {
                    _logger.LogError("Nome do blob inválido ou não encontrado na URL: {BlobUrl}", document.BlobStorageUrl);
                    throw new InvalidOperationException($"Nome do blob inválido ou não encontrado na URL: {document.BlobStorageUrl}");
                }
                
                var blobClient = containerClient.GetBlobClient(blobName);
                
                using var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                var documentBytes = memoryStream.ToArray();

                // Calcular hash do documento
                using var sha256 = SHA256.Create();
                var documentHash = sha256.ComputeHash(documentBytes);

                // Assinar o hash com o certificado do usuário
                var signature = await _keyVaultService.SignDataAsync(documentHash, userId);

                // Obter informações do certificado
                var certificateInfo = await _keyVaultService.GetUserCertificateInfoAsync(userId);

                // Criar registro de assinatura
                var documentSignature = new DocumentSignature
                {
                    DocumentId = documentId,
                    SignedBy = userId,
                    SignedByName = userName,
                    SignedByEmail = userEmail,
                    SignedAt = DateTime.UtcNow,
                    SignatureData = Convert.ToBase64String(signature),
                    DocumentHash = Convert.ToBase64String(documentHash),
                    CertificateThumbprint = certificateInfo.Thumbprint,
                    IsValid = true
                };

                // Salvar assinatura no Cosmos DB
                var savedSignature = await _cosmosService.SaveSignatureAsync(documentSignature);

                // Atualizar o documento para marcar como assinado
                document.IsDigitallySigned = true;
                await _documentService.UpdateDocumentAsync(document);

                // Registrar no log de auditoria
                await _auditService.LogDocumentActionAsync(
                    documentId,
                    userId,
                    "Sign",
                    null,
                    $"Documento assinado digitalmente. ID da assinatura: {savedSignature.Id}"
                );

                _logger.LogInformation("Documento {DocumentId} assinado com sucesso pelo usuário {UserId}", 
                    documentId, userId);

                return savedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao assinar documento {DocumentId} pelo usuário {UserId}", 
                    documentId, userId);
                throw;
            }
        }

        public async Task<bool> VerifySignatureAsync(int documentId, string signatureId)
        {
            try
            {
                // Buscar a assinatura
                var signature = await _cosmosService.GetSignatureAsync(signatureId);
                if (signature == null || signature.DocumentId != documentId)
                {
                    _logger.LogWarning("Assinatura {SignatureId} não encontrada para o documento {DocumentId}", 
                        signatureId, documentId);
                    return false;
                }

                // Buscar o documento
                var document = await _documentService.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    return false;
                }

                // Baixar o arquivo atual
                var containerClient = _blobServiceClient.GetBlobContainerClient("documents");
                
                var blobUri = new Uri(document.BlobStorageUrl);
                var pathAndQuery = blobUri.PathAndQuery;
                var blobName = pathAndQuery.Substring(pathAndQuery.IndexOf('/', 1) + 1);
                
                var blobClient = containerClient.GetBlobClient(blobName);
                
                using var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                var documentBytes = memoryStream.ToArray();

                // Calcular hash atual do documento
                using var sha256 = SHA256.Create();
                var currentHash = sha256.ComputeHash(documentBytes);

                // Comparar com o hash armazenado
                var storedHash = Convert.FromBase64String(signature.DocumentHash);
                if (!currentHash.SequenceEqual(storedHash))
                {
                    _logger.LogWarning("Hash do documento {DocumentId} não corresponde ao hash assinado", documentId);
                    return false;
                }

                // Verificar a assinatura com o Key Vault
                var signatureBytes = Convert.FromBase64String(signature.SignatureData);
                var isValid = await _keyVaultService.VerifySignatureAsync(currentHash, signatureBytes, signature.SignedBy);

                // Atualizar o status de verificação
                signature.IsValid = isValid;
                signature.VerifiedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Assinatura {SignatureId} verificada: {IsValid}", signatureId, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar assinatura {SignatureId}", signatureId);
                return false;
            }
        }

        public async Task<IEnumerable<DocumentSignature>> GetDocumentSignaturesAsync(int documentId)
        {
            try
            {
                return await _cosmosService.GetDocumentSignaturesAsync(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar assinaturas do documento {DocumentId}", documentId);
                throw;
            }
        }


    }
} 