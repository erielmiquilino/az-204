using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SecureDocManager.API.Data;
using SecureDocManager.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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

        public DocumentService(
            ApplicationDbContext context,
            IKeyVaultService keyVaultService,
            ICosmosService cosmosService,
            IConfiguration configuration,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _keyVaultService = keyVaultService;
            _cosmosService = cosmosService;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<BlobServiceClient> GetBlobServiceClientAsync()
        {
            if (_blobServiceClient == null)
            {
                var connectionString = await _keyVaultService.GetStorageConnectionStringAsync();
                _blobServiceClient = new BlobServiceClient(connectionString);
            }
            return _blobServiceClient;
        }

        private async Task<BlobContainerClient> GetContainerClientAsync()
        {
            var blobServiceClient = await GetBlobServiceClientAsync();
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            return containerClient;
        }

        public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string userId, string departmentId)
        {
            try
            {
                // Gerar nome único para o blob
                var blobName = $"{Guid.NewGuid()}/{fileName}";
                var containerClient = await GetContainerClientAsync();
                var blobClient = containerClient.GetBlobClient(blobName);

                // Upload do arquivo
                await blobClient.UploadAsync(fileStream, overwrite: true);

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
                    UploadedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // Criar registro no Cosmos DB para busca rápida
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

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do documento {FileName}", fileName);
                throw;
            }
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
                var blobName = new Uri(document.BlobStorageUrl).Segments.Last();
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
                var blobName = new Uri(document.BlobStorageUrl).Segments.Last();
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
    }
}
