using AgentPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Abstractions;

public interface IAgentDbContext
{
    DbSet<AgentTask> AgentTasks { get; }
    DbSet<Agent> Agents { get; }
    DbSet<Skill> Skills { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
