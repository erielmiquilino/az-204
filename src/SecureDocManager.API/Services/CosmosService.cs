using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using SecureDocManager.API.Models;
using System.Net;

namespace SecureDocManager.API.Services
{
    public class CosmosService : ICosmosService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _documentsContainer;
        private readonly Container _signaturesContainer;
        private readonly ILogger<CosmosService> _logger;

        public CosmosService(CosmosClient cosmosClient, ILogger<CosmosService> logger)
        {
            _cosmosClient = cosmosClient;
            _logger = logger;

            // Garante que o banco de dados e os contêineres existam.
            // Isso é útil para desenvolvimento e implantações iniciais.
            var databaseResponse = _cosmosClient.CreateDatabaseIfNotExistsAsync("DocumentsDB").GetAwaiter().GetResult();
            var database = databaseResponse.Database;
            _documentsContainer = database.CreateContainerIfNotExistsAsync("Documents", "/departmentId").GetAwaiter().GetResult();
            _signaturesContainer = database.CreateContainerIfNotExistsAsync("Signatures", "/documentId").GetAwaiter().GetResult();
        }

        public async Task<CosmosDocument> CreateDocumentAsync(CosmosDocument document)
        {
            try
            {
                var response = await _documentsContainer.CreateItemAsync(
                    document, 
                    new PartitionKey(document.DepartmentId));
                
                _logger.LogInformation("Documento criado no Cosmos DB: {DocumentId}", document.Id);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar documento no Cosmos DB");
                throw;
            }
        }

        public async Task<CosmosDocument?> GetDocumentAsync(string id, string departmentId)
        {
            try
            {
                var response = await _documentsContainer.ReadItemAsync<CosmosDocument>(
                    id, 
                    new PartitionKey(departmentId));
                
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Documento não encontrado: {DocumentId}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar documento {DocumentId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CosmosDocument>> GetDocumentsByDepartmentAsync(string departmentId, string userRole)
        {
            try
            {
                var accessLevel = GetAccessLevel(userRole);
                
                var queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.departmentId = @departmentId AND c.accessLevel <= @accessLevel ORDER BY c.uploadedAt DESC")
                    .WithParameter("@departmentId", departmentId)
                    .WithParameter("@accessLevel", accessLevel);
                
                var query = _documentsContainer.GetItemQueryIterator<CosmosDocument>(queryDefinition);
                var results = new List<CosmosDocument>();
                
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }
                
                _logger.LogInformation(
                    "Encontrados {Count} documentos para o departamento {DepartmentId}", 
                    results.Count, 
                    departmentId);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar documentos do departamento {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<CosmosDocument> UpdateDocumentAsync(CosmosDocument document)
        {
            try
            {
                var response = await _documentsContainer.ReplaceItemAsync(
                    document, 
                    document.Id, 
                    new PartitionKey(document.DepartmentId));
                
                _logger.LogInformation("Documento atualizado: {DocumentId}", document.Id);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar documento {DocumentId}", document.Id);
                throw;
            }
        }

        public async Task DeleteDocumentAsync(string id, string departmentId)
        {
            try
            {
                await _documentsContainer.DeleteItemAsync<CosmosDocument>(
                    id, 
                    new PartitionKey(departmentId));
                
                _logger.LogInformation("Documento deletado: {DocumentId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar documento {DocumentId}", id);
                throw;
            }
        }

        public async Task AddAccessHistoryAsync(string documentId, string departmentId, AccessHistoryEntry entry)
        {
            try
            {
                var document = await GetDocumentAsync(documentId, departmentId);
                if (document != null)
                {
                    document.AccessHistory.Add(entry);
                    
                    // Manter apenas os últimos 100 registros de histórico
                    if (document.AccessHistory.Count > 100)
                    {
                        document.AccessHistory = document.AccessHistory
                            .OrderByDescending(h => h.Timestamp)
                            .Take(100)
                            .ToList();
                    }
                    
                    await UpdateDocumentAsync(document);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar histórico de acesso");
                // Não propagar o erro para não afetar a operação principal
            }
        }

        public async Task<IEnumerable<CosmosDocument>> SearchDocumentsAsync(string searchTerm, string departmentId, string userRole)
        {
            try
            {
                var accessLevel = GetAccessLevel(userRole);
                
                var queryDefinition = new QueryDefinition(
                    @"SELECT * FROM c 
                      WHERE c.departmentId = @departmentId 
                      AND c.accessLevel <= @accessLevel 
                      AND (CONTAINS(LOWER(c.fileName), LOWER(@searchTerm)) 
                           OR ARRAY_CONTAINS(c.tags, @searchTerm))
                      ORDER BY c.uploadedAt DESC")
                    .WithParameter("@departmentId", departmentId)
                    .WithParameter("@accessLevel", accessLevel)
                    .WithParameter("@searchTerm", searchTerm);
                
                var query = _documentsContainer.GetItemQueryIterator<CosmosDocument>(queryDefinition);
                var results = new List<CosmosDocument>();
                
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao pesquisar documentos");
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
        
        // Implementação dos métodos de assinatura
        public async Task<DocumentSignature> SaveSignatureAsync(DocumentSignature signature)
        {
            try
            {
                // Garante que o ID da assinatura seja único
                if (string.IsNullOrEmpty(signature.Id))
                {
                    signature.Id = Guid.NewGuid().ToString();
                }

                var response = await _signaturesContainer.CreateItemAsync(
                    signature, 
                    new PartitionKey(signature.DocumentId));
                
                _logger.LogInformation("Assinatura salva para o documento: {DocumentId}", signature.DocumentId);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar assinatura do documento {DocumentId}", signature.DocumentId);
                throw;
            }
        }

        public async Task<DocumentSignature?> GetSignatureAsync(string signatureId)
        {
            try
            {
                 var queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.id = @signatureId")
                    .WithParameter("@signatureId", signatureId);
                
                // Como não sabemos a partition key (documentId), fazemos uma cross-partition query
                var queryOptions = new QueryRequestOptions { MaxConcurrency = -1 };

                var query = _signaturesContainer.GetItemQueryIterator<DocumentSignature>(queryDefinition, requestOptions: queryOptions);
                
                if (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    return response.FirstOrDefault();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar assinatura {SignatureId}", signatureId);
                throw;
            }
        }

        public async Task<IEnumerable<DocumentSignature>> GetDocumentSignaturesAsync(int documentId)
        {
            try
            {
                var queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c.signedAt DESC");
                
                var queryOptions = new QueryRequestOptions 
                { 
                    PartitionKey = new PartitionKey(documentId) 
                };

                var query = _signaturesContainer.GetItemQueryIterator<DocumentSignature>(queryDefinition, requestOptions: queryOptions);
                var results = new List<DocumentSignature>();

                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response.ToList());
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar assinaturas do documento {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<bool> DocumentHasSignaturesAsync(int documentId)
        {
             try
            {
                var queryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                var queryOptions = new QueryRequestOptions 
                { 
                    PartitionKey = new PartitionKey(documentId) 
                };

                var query = _signaturesContainer.GetItemQueryIterator<int>(queryDefinition, requestOptions: queryOptions);
                
                if (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    return response.FirstOrDefault() > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar a existência de assinaturas para o documento {DocumentId}", documentId);
                return false; // Assumir que não há assinaturas em caso de erro
            }
        }
    }
}
