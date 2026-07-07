using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Data;

public class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options)
{
    public DbSet<AgentJob> Jobs => Set<AgentJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentJob>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Status).HasConversion<string>();
        });
    }
}
