using MediatR;

namespace AgentPlatform.Application.Features.Tasks.CreateTask;

public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, CreateTaskResult>
{
    public Task<CreateTaskResult> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var result = new CreateTaskResult(
            Response: $"Task accepted for system '{request.System}'.",
            Action: request.Action);

        return Task.FromResult(result);
    }
}
