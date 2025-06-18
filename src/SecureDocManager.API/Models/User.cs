using System.ComponentModel.DataAnnotations;

namespace SecureDocManager.API.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = string.Empty; // Azure AD Object ID

        [Required]
        [StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "Employee"; // Admin, Manager, Employee

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navegação
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
