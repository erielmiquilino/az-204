namespace SecureDocManager.API.Services
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
        Task<string> GetConnectionStringAsync();
        Task<string> GetStorageConnectionStringAsync();
        Task<string> GetCosmosConnectionStringAsync();
        Task<byte[]> GetCertificateAsync(string certificateName);
    }
}
