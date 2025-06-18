using GraphUser = Microsoft.Graph.Models.User;

namespace SecureDocManager.API.Services
{
    public interface IGraphService
    {
        Task<GraphUser?> GetUserProfileAsync(string userId);
        Task<IEnumerable<GraphUser>> GetUsersInDepartmentAsync(string department);
        Task<string?> GetUserDepartmentAsync(string userId);
        Task<byte[]?> GetUserPhotoAsync(string userId);
    }
}
