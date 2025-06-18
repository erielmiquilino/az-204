using Newtonsoft.Json;

namespace SecureDocManager.API.Models
{
    public class CosmosDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("documentId")]
        public int DocumentId { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty("departmentId")]
        public string DepartmentId { get; set; } = string.Empty;

        [JsonProperty("uploadedByUserId")]
        public string UploadedByUserId { get; set; } = string.Empty;

        [JsonProperty("uploadedByUserName")]
        public string UploadedByUserName { get; set; } = string.Empty;

        [JsonProperty("uploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("accessLevel")]
        public int AccessLevel { get; set; } = 1;

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        [JsonProperty("accessHistory")]
        public List<AccessHistoryEntry> AccessHistory { get; set; } = new List<AccessHistoryEntry>();

        [JsonProperty("_etag")]
        public string? ETag { get; set; }
    }

    public class AccessHistoryEntry
    {
        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty; // View, Download, Edit, Delete

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("ipAddress")]
        public string? IpAddress { get; set; }
    }
}
