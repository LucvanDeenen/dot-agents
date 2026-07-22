using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Data;

public class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options), IAgentDbContext
{
    public DbSet<AgentTask> AgentTasks => Set<AgentTask>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Skill> Skills => Set<Skill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTask>(entity =>
        {
            entity.Property(t => t.RoutingKey).HasMaxLength(256);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasOne(t => t.Agent)
                .WithMany()
                .HasForeignKey(t => t.AgentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(a => a.Name).HasMaxLength(128);
            entity.HasIndex(a => a.Name).IsUnique();
            entity.Property(a => a.RoutingKeyPattern).HasMaxLength(256);
            entity.HasMany(a => a.Skills).WithMany(s => s.Agents);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(128);
            entity.HasIndex(s => s.Name).IsUnique();
        });
    }
}
