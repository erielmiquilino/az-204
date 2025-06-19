namespace SecureDocManager.API.Services
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
        Task<string> GetConnectionStringAsync();
        Task<string> GetStorageConnectionStringAsync();
        Task<string> GetCosmosConnectionStringAsync();
        Task<byte[]> GetCertificateAsync(string certificateName);
        
        // Novos métodos para assinatura digital
        Task<bool> CreateUserCertificateAsync(string userId, string userEmail, string userName);
        Task<bool> UserCertificateExistsAsync(string userId);
        Task<byte[]> SignDataAsync(byte[] hash, string userId);
        Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, string userId);
        Task<CertificateInfo> GetUserCertificateInfoAsync(string userId);
    }
    
    public class CertificateInfo
    {
        public string Thumbprint { get; set; } = string.Empty;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string Subject { get; set; } = string.Empty;
        public bool IsValid => DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;
    }
}
