using Microsoft.EntityFrameworkCore;
using SecureDocManager.API.Models;

namespace SecureDocManager.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade Document
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(50);
                entity.Property(e => e.BlobStorageUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DepartmentId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UploadedByUserId).IsRequired();
                entity.HasIndex(e => e.DepartmentId);
                entity.HasIndex(e => e.UploadedByUserId);
                entity.HasIndex(e => e.IsDeleted);
            });

            // Configuração da entidade User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            });

            // Configuração da entidade AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}
