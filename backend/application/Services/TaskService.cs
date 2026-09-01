using AgentPlatform.Application.Agents;

namespace AgentPlatform.Application.Services;

public interface IAgentService
{
    Task<string> Prompt(string message, CancellationToken ct);
}

public class AgentService(IAgentRunner agentRunner) : IAgentService
{
    public async Task<string> Prompt(string message, CancellationToken ct)
    {
        var agent = AgentConfig.ForTask(action: message, context: null, system: null);
        var reply = await agentRunner.StartAsync(agent, ct);
        
        return await Task.FromResult(reply.ToString());
    }
}