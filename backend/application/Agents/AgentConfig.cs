namespace AgentPlatform.Application.Agents;

/// <summary>
/// The instruction handed to a single agent run: which agent to act as and
/// what to do. <see cref="Instruction"/> is sent straight to `claude -p` inside
/// the runner container.
/// </summary>
public sealed record AgentConfig
{
    private const string DefaultAgentName = "default";

    public List<AgentReply> Chat { get; }
    public string AgentName { get; }
    public string Instruction { get; }

    private AgentConfig(string agentName, string instruction, List<AgentReply> chat)
    {
        AgentName = agentName;
        Instruction = instruction;
        Chat = chat;
    }

    public static AgentConfig ForTask(string? system, string? action, string? context)
    {
        var agentName = string.IsNullOrWhiteSpace(system) ? DefaultAgentName : system.Trim();
        var instruction = BuildInstruction(action, context);
        return new AgentConfig(agentName, instruction, []);
    }

    private static string BuildInstruction(string? action, string? context)
    {
        var a = string.IsNullOrWhiteSpace(action) ? "(no action specified)" : action.Trim();
        return string.IsNullOrWhiteSpace(context) ? a : $"{a}\n\nContext:\n{context.Trim()}";
    }
}