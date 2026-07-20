using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Jobs;

public sealed record ListJobsQuery : IRequest<List<AgentTask>>;

public sealed class ListJobsQueryHandler(IAgentDbContext db) : IRequestHandler<ListJobsQuery, List<AgentTask>>
{
    public async Task<List<AgentTask>> Handle(ListJobsQuery request, CancellationToken cancellationToken)
        => await db.AgentTasks
            .Include(t => t.Agent)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
}
