namespace AgentPlatform.Application.Agents;

/// <summary>
/// The full instruction set handed to a single agent run. Serialized to the
/// base64 <c>RUN_CONFIG</c> payload that the runner container's setup.mjs
/// materializes — property names here must match the JSON keys it reads
/// (agentName, instruction, systemPrompt, allowedTools, skills, repoUrl, branch).
/// </summary>
public record AgentRunConfig(
    string AgentName,
    string Instruction,
    string? SystemPrompt = null,
    IReadOnlyList<string>? AllowedTools = null,
    IReadOnlyList<AgentSkill>? Skills = null,
    string? RepoUrl = null,
    string? Branch = null);

/// <summary>A skill materialized into the run container as a Claude Code skill.</summary>
public record AgentSkill(string Name, string? Description, string Instructions);
