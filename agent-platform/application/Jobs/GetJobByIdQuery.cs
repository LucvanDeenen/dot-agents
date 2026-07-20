using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Application.Jobs;

public sealed record GetJobByIdQuery(Guid Id) : IRequest<AgentTask?>;

public sealed class GetJobByIdQueryHandler(IAgentDbContext db) : IRequestHandler<GetJobByIdQuery, AgentTask?>
{
    public async Task<AgentTask?> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
        => await db.AgentTasks
            .Include(t => t.Agent)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
}
