using Newtonsoft.Json;

namespace SecureDocManager.API.Models
{
    public class DocumentSignature
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("documentId")]
        public int DocumentId { get; set; }

        [JsonProperty("signedBy")]
        public string SignedBy { get; set; } = string.Empty;

        [JsonProperty("signedByName")]
        public string SignedByName { get; set; } = string.Empty;

        [JsonProperty("signedByEmail")]
        public string SignedByEmail { get; set; } = string.Empty;

        [JsonProperty("signedAt")]
        public DateTime SignedAt { get; set; }

        [JsonProperty("signatureData")]
        public string SignatureData { get; set; } = string.Empty;

        [JsonProperty("documentHash")]
        public string DocumentHash { get; set; } = string.Empty;

        [JsonProperty("certificateThumbprint")]
        public string CertificateThumbprint { get; set; } = string.Empty;

        [JsonProperty("hashAlgorithm")]
        public string HashAlgorithm { get; set; } = "SHA256";

        [JsonProperty("signatureAlgorithm")]
        public string SignatureAlgorithm { get; set; } = "RS256";

        [JsonProperty("isValid")]
        public bool IsValid { get; set; }

        [JsonProperty("verifiedAt")]
        public DateTime? VerifiedAt { get; set; }

        [JsonProperty("type")]
        public string Type => "DocumentSignature";
    }
} 