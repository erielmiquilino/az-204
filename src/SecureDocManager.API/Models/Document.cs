using System.ComponentModel.DataAnnotations;

namespace SecureDocManager.API.Models
{
    public class Document
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FileExtension { get; set; } = string.Empty;

        public long FileSizeInBytes { get; set; }

        [Required]
        [StringLength(500)]
        public string BlobStorageUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DepartmentId { get; set; } = string.Empty;

        [Required]
        public string UploadedByUserId { get; set; } = string.Empty;

        [StringLength(200)]
        public string UploadedByUserName { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedAt { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int AccessLevel { get; set; } = 1; // 1=Employee, 2=Manager, 3=Admin

        public bool IsDeleted { get; set; } = false;

        [StringLength(500)]
        public string? Tags { get; set; } // Comma-separated tags

        // Propriedades de assinatura digital
        public bool IsSigned { get; set; } = false;
        public DateTime? SignedAt { get; set; }
        public string? SignedByUserId { get; set; }
        public string? SignatureData { get; set; }
    }
}
