using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using SecureDocManager.API.Models;
using System.Net;

namespace SecureDocManager.API.Services
{
    public class CosmosService : ICosmosService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly ILogger<CosmosService> _logger;

        public CosmosService(CosmosClient cosmosClient, ILogger<CosmosService> logger)
        {
            _cosmosClient = cosmosClient;
            _logger = logger;
            _container = _cosmosClient.GetContainer("DocumentsDB", "Documents");
        }

        public async Task<CosmosDocument> CreateDocumentAsync(CosmosDocument document)
        {
            try
            {
                var response = await _container.CreateItemAsync(
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
                var response = await _container.ReadItemAsync<CosmosDocument>(
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
                
                var query = _container.GetItemQueryIterator<CosmosDocument>(queryDefinition);
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
                var response = await _container.ReplaceItemAsync(
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
                await _container.DeleteItemAsync<CosmosDocument>(
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
                
                var query = _container.GetItemQueryIterator<CosmosDocument>(queryDefinition);
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
    }
}
