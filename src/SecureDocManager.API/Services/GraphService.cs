using Microsoft.Graph;
using GraphUser = Microsoft.Graph.Models.User;
using Microsoft.Identity.Web;

namespace SecureDocManager.API.Services
{
    public class GraphService : IGraphService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<GraphService> _logger;

        public GraphService(GraphServiceClient graphClient, ILogger<GraphService> logger)
        {
            _graphClient = graphClient;
            _logger = logger;
        }

        public async Task<GraphUser?> GetUserProfileAsync(string userId)
        {
            try
            {
                var user = await _graphClient.Users[userId]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] 
                        { 
                            "id", 
                            "displayName", 
                            "mail", 
                            "department", 
                            "jobTitle", 
                            "userPrincipalName" 
                        };
                    });

                return user;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Erro ao obter perfil do usuário {UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<GraphUser>> GetUsersInDepartmentAsync(string department)
        {
            try
            {
                var users = await _graphClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"department eq '{department}'";
                        requestConfiguration.QueryParameters.Select = new[] 
                        { 
                            "id", 
                            "displayName", 
                            "mail", 
                            "department", 
                            "jobTitle" 
                        };
                    });

                return users?.Value ?? new List<GraphUser>();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Erro ao obter usuários do departamento {Department}", department);
                return new List<GraphUser>();
            }
        }

        public async Task<string?> GetUserDepartmentAsync(string userId)
        {
            try
            {
                var user = await _graphClient.Users[userId]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "department" };
                    });

                return user?.Department;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Erro ao obter departamento do usuário {UserId}", userId);
                return null;
            }
        }

        public async Task<byte[]?> GetUserPhotoAsync(string userId)
        {
            try
            {
                var photoStream = await _graphClient.Users[userId].Photo.Content
                    .GetAsync();

                if (photoStream != null)
                {
                    using var memoryStream = new MemoryStream();
                    await photoStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }

                return null;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Erro ao obter foto do usuário {UserId}", userId);
                return null;
            }
        }
    }
}
