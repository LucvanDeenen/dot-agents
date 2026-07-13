using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Data;

public class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options), IAgentDbContext
{
    public DbSet<AgentTask> AgentTasks => Set<AgentTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTask>(entity =>
        {
            entity.Property(t => t.RoutingKey).HasMaxLength(256);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
        });
    }
}
