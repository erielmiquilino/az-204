using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SecureDocManager.API.Data;
using SecureDocManager.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Azure.Core;
using Polly;
using Polly.Extensions.Http;

namespace SecureDocManager.API.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ICosmosService _cosmosService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentService> _logger;
        private BlobServiceClient? _blobServiceClient;
        private readonly string _containerName = "documents";
        private readonly HttpClient _httpClient;

        public DocumentService(
            ApplicationDbContext context,
            IKeyVaultService keyVaultService,
            ICosmosService cosmosService,
            IConfiguration configuration,
            ILogger<DocumentService> logger,
            HttpClient httpClient)
        {
            _context = context;
            _keyVaultService = keyVaultService;
            _cosmosService = cosmosService;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        private async Task<BlobServiceClient> GetBlobServiceClientAsync()
        {
            if (_blobServiceClient == null)
            {
                try
                {
                    var connectionString = await _keyVaultService.GetStorageConnectionStringAsync();
                    
                    // Configurar opções do cliente com timeout e retry policy mais simples
                    var options = new BlobClientOptions()
                    {
                        Retry = {
                            MaxRetries = 2,
                            Delay = TimeSpan.FromSeconds(1),
                            MaxDelay = TimeSpan.FromSeconds(5),
                            Mode = RetryMode.Fixed
                        }
                    };
                    
                    _blobServiceClient = new BlobServiceClient(connectionString, options);
                    
                    _logger.LogInformation("BlobServiceClient criado com sucesso");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar BlobServiceClient");
                    throw;
                }
            }
            return _blobServiceClient;
        }

        private async Task<BlobContainerClient> GetContainerClientAsync()
        {
            var maxRetries = 3;
            var baseDelay = TimeSpan.FromSeconds(1);
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var blobServiceClient = await GetBlobServiceClientAsync();
                    var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                    
                    // Verificar se o container existe antes de tentar criar
                    var exists = await containerClient.ExistsAsync();
                    if (!exists.Value)
                    {
                        _logger.LogInformation("Container '{ContainerName}' não existe, criando...", _containerName);
                        var response = await containerClient.CreateAsync(PublicAccessType.None);
                        _logger.LogInformation("Container '{ContainerName}' criado com sucesso", _containerName);
                    }
                    else
                    {
                        _logger.LogDebug("Container '{ContainerName}' já existe", _containerName);
                    }

                    return containerClient;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tentativa {Attempt} de {MaxRetries} falhou ao obter container '{ContainerName}'", 
                        attempt, maxRetries, _containerName);
                    
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Falha final ao obter/criar container '{ContainerName}' após {MaxRetries} tentativas", 
                            _containerName, maxRetries);
                        throw;
                    }
                    
                    // Esperar antes da próxima tentativa com backoff exponencial
                    var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }
            
            throw new InvalidOperationException("Não deveria chegar aqui");
        }

        public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string userId, string userName, string departmentId)
        {
            try
            {
                _logger.LogInformation("Iniciando upload do documento {FileName} para o usuário {UserId}", fileName, userId);
                
                // Validar parâmetros
                if (fileStream == null || !fileStream.CanRead)
                    throw new ArgumentException("Stream de arquivo inválido");
                
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Nome do arquivo é obrigatório");
                
                // Gerar nome único para o blob
                var blobName = $"{departmentId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{fileName}";
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(blobName);

                // Configurar opções de upload
                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = GetContentType(fileName)
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        {"UploadedBy", userId},
                        {"DepartmentId", departmentId},
                        {"UploadedAt", DateTime.UtcNow.ToString("O")}
                    }
                };

                // Upload do arquivo com retry manual
                var maxUploadRetries = 2;
                Exception? lastException = null;
                
                for (int attempt = 1; attempt <= maxUploadRetries; attempt++)
                {
                    try
                    {
                        fileStream.Position = 0; // Reset stream position
                        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken: CancellationToken.None);
                        _logger.LogInformation("Upload concluído com sucesso para {FileName} na tentativa {Attempt}", fileName, attempt);
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Tentativa {Attempt} de upload falhou para {FileName}", attempt, fileName);
                        
                        if (attempt < maxUploadRetries)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                        }
                    }
                }
                
                if (lastException != null && !await blobClient.ExistsAsync())
                {
                    throw lastException;
                }

                // Obter informações do arquivo
                var properties = await blobClient.GetPropertiesAsync();

                // Criar registro no SQL Database
                var document = new Document
                {
                    FileName = fileName,
                    FileExtension = Path.GetExtension(fileName),
                    FileSizeInBytes = properties.Value.ContentLength,
                    BlobStorageUrl = blobClient.Uri.ToString(),
                    DepartmentId = departmentId,
                    UploadedByUserId = userId,
                    UploadedByUserName = userName,
                    UploadedAt = DateTime.UtcNow,
                    ContentType = GetContentType(fileName)
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Documento {DocumentId} salvo no banco de dados", document.Id);

                // Criar registro no Cosmos DB para busca rápida (não crítico)
                try
                {
                    var cosmosDocument = new CosmosDocument
                    {
                        DocumentId = document.Id,
                        FileName = document.FileName,
                        DepartmentId = departmentId,
                        UploadedByUserId = userId,
                        UploadedAt = document.UploadedAt,
                        AccessLevel = document.AccessLevel
                    };

                    await _cosmosService.CreateDocumentAsync(cosmosDocument);
                    _logger.LogInformation("Documento {DocumentId} salvo no Cosmos DB", document.Id);
                }
                catch (Exception cosmosEx)
                {
                    _logger.LogWarning(cosmosEx, "Falha ao salvar no Cosmos DB, mas documento foi salvo no SQL");
                    // Não falhar o upload por causa do Cosmos DB
                }

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do documento {FileName}", fileName);
                throw;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        public async Task<string> GenerateDownloadUrlAsync(string documentId, string userRole)
        {
            try
            {
                var document = await _context.Documents.FindAsync(int.Parse(documentId));
                if (document == null)
                {
                    throw new FileNotFoundException("Documento não encontrado");
                }

                var containerClient = await GetContainerClientAsync();
                
                var blobUri = new Uri(document.BlobStorageUrl);
                var pathAndQuery = blobUri.PathAndQuery;
                var blobName = pathAndQuery.Substring(pathAndQuery.IndexOf('/', 1) + 1);
                
                // Decodificar o nome do blob para lidar com espaços e caracteres especiais
                blobName = Uri.UnescapeDataString(blobName);

                var blobClient = containerClient.GetBlobClient(blobName);

                // Verificar se o blob existe
                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException("Arquivo não encontrado no storage");
                }

                // Gerar SAS token com permissões baseadas no role
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                // Diferentes permissões baseadas no role
                if (userRole == "Admin" || userRole == "Manager")
                {
                    sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write);
                }
                else
                {
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);
                }

                // Gerar a URL com SAS
                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                
                return sasUri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar URL de download para documento {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<string> GenerateUploadUrlAsync(string fileName, string departmentId)
        {
            try
            {
                var blobName = $"{departmentId}/{Guid.NewGuid()}/{fileName}";
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(blobName);

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerName,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar URL de upload");
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    return false;
                }

                // Soft delete no banco
                document.IsDeleted = true;
                await _context.SaveChangesAsync();

                // Opcionalmente, deletar do blob storage
                // var containerClient = await GetContainerClientAsync();
                // var blobName = new Uri(document.BlobStorageUrl).Segments.Last();
                // await containerClient.DeleteBlobIfExistsAsync(blobName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar documento {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<Document?> GetDocumentByIdAsync(int documentId)
        {
            return await _context.Documents
                .Where(d => d.Id == documentId && !d.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Document>> GetDocumentsByDepartmentAsync(string departmentId, string userRole)
        {
            var query = _context.Documents
                .Where(d => d.DepartmentId == departmentId && !d.IsDeleted);

            // Filtrar por nível de acesso baseado no role
            var accessLevel = GetAccessLevel(userRole);
            query = query.Where(d => d.AccessLevel <= accessLevel);

            return await query
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync(string userRole)
        {
            var query = _context.Documents
                .Where(d => !d.IsDeleted);

            // Filtrar por nível de acesso baseado no role
            var accessLevel = GetAccessLevel(userRole);
            query = query.Where(d => d.AccessLevel <= accessLevel);

            return await query
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<byte[]> SignDocumentAsync(byte[] document, string certificateName)
        {
            try
            {
                var certificate = await _keyVaultService.GetCertificateAsync(certificateName);
                
                // Implementar assinatura digital
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(certificate, out _);
                
                var signature = rsa.SignData(document, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                
                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao assinar documento");
                throw;
            }
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

        public async Task<string> GetUserRoleAsync(string userId)
        {
            // Em produção, isso viria do Microsoft Graph ou do banco de dados
            // Por enquanto, vamos simular baseado no ID do usuário
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user != null)
            {
                return user.Role;
            }

            // Retornar Employee como padrão
            return "Employee";
        }

        public async Task<byte[]> DownloadDocumentAsync(int documentId)
        {
            try
            {
                var document = await GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    throw new FileNotFoundException("Documento não encontrado");
                }

                var containerClient = await GetContainerClientAsync();

                var blobUri = new Uri(document.BlobStorageUrl);
                var pathAndQuery = blobUri.PathAndQuery;
                var blobName = pathAndQuery.Substring(pathAndQuery.IndexOf('/', 1) + 1);
                
                // Decodificar o nome do blob para lidar com espaços e caracteres especiais
                blobName = Uri.UnescapeDataString(blobName);

                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException("Arquivo não encontrado no storage");
                }

                // Download do conteúdo do blob
                var response = await blobClient.DownloadContentAsync();
                return response.Value.Content.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer download do documento {DocumentId}", documentId);
                throw;
            }
        }
        
        public async Task<Document> UpdateDocumentAsync(Document document)
        {
            try
            {
                _context.Documents.Update(document);
                await _context.SaveChangesAsync();
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar documento {DocumentId}", document.Id);
                throw;
            }
        }
    }
}
