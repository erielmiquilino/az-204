using System.ComponentModel.DataAnnotations;

namespace SecureDocManager.API.Models.DTOs
{
    public class DocumentUploadDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentId { get; set; } = string.Empty;

        public int AccessLevel { get; set; } = 1;

        public string? Tags { get; set; }
    }

    public class DocumentResponseDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public string UploadedByUserName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
        public int AccessLevel { get; set; }
        public bool IsSigned { get; set; }
        public string? DownloadUrl { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class DocumentSignDto
    {
        [Required]
        public int DocumentId { get; set; }

        [Required]
        public string CertificateName { get; set; } = string.Empty;
    }
}
