using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureDocManager.API.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string BlobStorageUrl { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string UploadedByUserId { get; set; } = string.Empty;
        public string UploadedByUserName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
        public int AccessLevel { get; set; }
        public bool IsDigitallySigned { get; set; }
        public string? Tags { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
