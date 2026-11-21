using Microsoft.EntityFrameworkCore;
using StudentApi.Models;

namespace StudentApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FullName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(s => s.Email).IsUnique();
            entity.Property(s => s.Department).HasMaxLength(80).IsRequired();
            entity.HasIndex(s => s.Department);
        });
    }
}
