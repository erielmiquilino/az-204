using Microsoft.Graph.Models;

namespace SecureDocManager.API.Services
{
    public class MockGraphService : IGraphService
    {
        public Task<User?> GetUserProfileAsync(string userId)
        {
            return Task.FromResult<User?>(new User
            {
                Id = userId,
                DisplayName = "Mock User",
                Mail = "mockuser@example.com",
                Department = "IT",
                JobTitle = "Developer"
            });
        }

        public Task<IEnumerable<User>> GetUsersInDepartmentAsync(string department)
        {
            return Task.FromResult<IEnumerable<User>>(new List<User>());
        }

        public Task<string?> GetUserDepartmentAsync(string userId)
        {
            return Task.FromResult<string?>("IT");
        }

        public Task<byte[]?> GetUserPhotoAsync(string userId)
        {
            return Task.FromResult<byte[]?>(null);
        }
    }
} 