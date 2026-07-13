using AdminService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Api.Data;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<RoleChangeAudit> RoleChangeAudits => Set<RoleChangeAudit>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admin");

        modelBuilder.Entity<RoleChangeAudit>(entity =>
        {
            entity.ToTable("role_change_audits");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.ChangedAtUtc);
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.ToTable("system_configs");
            entity.HasKey(c => c.Key);
            entity.Property(c => c.Key).HasMaxLength(128);
        });
    }
}
