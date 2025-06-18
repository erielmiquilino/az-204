using Microsoft.Azure.Cosmos;
using SecureDocManager.API.Models;

namespace SecureDocManager.API.Data
{
    public class CosmosDbContext
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseName = "DocumentsDB";
        private readonly string _containerName = "Documents";
        private Container? _container;

        public CosmosDbContext(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        public async Task<Container> GetContainerAsync()
        {
            if (_container == null)
            {
                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
                
                var containerProperties = new ContainerProperties
                {
                    Id = _containerName,
                    PartitionKeyPath = "/departmentId"
                };

                var container = await database.Database.CreateContainerIfNotExistsAsync(
                    containerProperties,
                    throughput: 400);

                _container = container.Container;
            }

            return _container;
        }

        public async Task<CosmosDocument> CreateDocumentAsync(CosmosDocument document)
        {
            var container = await GetContainerAsync();
            var response = await container.CreateItemAsync(
                document, 
                new PartitionKey(document.DepartmentId));
            return response.Resource;
        }

        public async Task<CosmosDocument?> GetDocumentAsync(string id, string departmentId)
        {
            try
            {
                var container = await GetContainerAsync();
                var response = await container.ReadItemAsync<CosmosDocument>(
                    id, 
                    new PartitionKey(departmentId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<CosmosDocument> UpdateDocumentAsync(CosmosDocument document)
        {
            var container = await GetContainerAsync();
            var response = await container.ReplaceItemAsync(
                document, 
                document.Id, 
                new PartitionKey(document.DepartmentId));
            return response.Resource;
        }

        public async Task DeleteDocumentAsync(string id, string departmentId)
        {
            var container = await GetContainerAsync();
            await container.DeleteItemAsync<CosmosDocument>(
                id, 
                new PartitionKey(departmentId));
        }
    }
}
