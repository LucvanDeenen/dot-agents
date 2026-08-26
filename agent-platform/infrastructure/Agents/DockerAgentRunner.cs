using System.Text;
using System.Text.Json;
using AgentPlatform.Application.Agents;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentPlatform.Infrastructure.Agents;

/// <summary>
/// Instantiates an agent by starting one long-lived <c>agent-runner</c> container
/// per run. The container materializes its workspace, then idles (up to the
/// configured lifetime) so its Claude Code session persists on disk. Each turn —
/// the first one and every follow-up — is driven with `docker exec run.sh`,
/// which invokes `claude -p --output-format json`; follow-ups add `--continue`
/// to resume the same session. The run is addressed by a runId baked into the
/// container name, so no separate registry is needed.
/// </summary>
public sealed class DockerAgentRunner(
    IDockerClient docker,
    IOptions<AgentRunnerOptions> options,
    ILogger<DockerAgentRunner> logger) : IAgentRunner
{
    private const string RunScript = "/home/agent/run.sh";
    private const string NamePrefix = "agent-run-";

    // camelCase so the payload keys line up with what the runner's setup.mjs reads.
    private static readonly JsonSerializerOptions RunConfigJson = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AgentRunnerOptions _options = options.Value;

    public async Task<AgentReply> StartAsync(AgentRunConfig config, CancellationToken ct)
    {
        var runId = Guid.NewGuid().ToString("N");

        var runConfig = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(config, RunConfigJson)));

        var env = new List<string>
        {
            $"RUN_CONFIG={runConfig}",
            $"AGENT_MAX_LIFETIME_SECONDS={_options.RunTimeoutMinutes * 60}"
        };

        var token = _options.ClaudeCodeOAuthToken
                    ?? Environment.GetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            env.Add($"CLAUDE_CODE_OAUTH_TOKEN={token}");
        else
            logger.LogWarning("No CLAUDE_CODE_OAUTH_TOKEN available; the runner will fail to authenticate.");

        var created = await docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = NamePrefix + runId,
            Image = _options.Image,
            Env = env,
            Labels = new Dictionary<string, string>
            {
                ["agent-platform.run"] = runId,
                ["agent-platform.agent"] = config.AgentName
            }
        }, ct);

        await docker.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(), ct);
        logger.LogInformation("Started agent '{Agent}' as run {RunId} (container {ContainerId})",
            config.AgentName, runId, created.ID);

        // First turn: empty message → the container uses its generated guide prompt.
        var response = await ExecTurnAsync(NamePrefix + runId, message: "", resume: false, ct);
        return new AgentReply(runId, response);
    }

    public async Task<AgentReply> ContinueAsync(string runId, string message, CancellationToken ct)
    {
        var response = await ExecTurnAsync(NamePrefix + runId, message, resume: true, ct);
        return new AgentReply(runId, response);
    }

    /// <summary>Run one Claude turn inside the container via `docker exec run.sh` and return its text result.</summary>
    private async Task<string> ExecTurnAsync(string containerName, string message, bool resume, CancellationToken ct)
    {
        var mode = resume ? "continue" : "start";

        ContainerExecCreateResponse exec;
        try
        {
            exec = await docker.Exec.ExecCreateContainerAsync(containerName, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                // Invoke via bash so the script doesn't depend on its +x bit surviving checkout.
                Cmd = new List<string> { "/bin/bash", RunScript, message, mode }
            }, ct);
        }
        catch (DockerContainerNotFoundException)
        {
            // Container gone (never started, or self-terminated past its lifetime).
            throw new AgentRunNotFoundException(TrimName(containerName));
        }
        catch (DockerApiException ex) when (
            ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Conflict)
        {
            // 404 = container removed; 409 = exists but no longer running (past its lifetime).
            throw new AgentRunNotFoundException(TrimName(containerName));
        }

        var (stdout, stderr) = await RunExecAsync(exec.ID, ct);

        var inspect = await docker.Exec.InspectContainerExecAsync(exec.ID, ct);
        if (inspect.ExitCode != 0)
            logger.LogWarning("Run turn exited {Exit} for {Container}: {Stderr}",
                inspect.ExitCode, TrimName(containerName), stderr);

        return ParseResult(stdout, stderr);
    }

    private async Task<(string Stdout, string Stderr)> RunExecAsync(string execId, CancellationToken ct)
    {
        using var stream = await docker.Exec.StartAndAttachContainerExecAsync(execId, tty: false, ct);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var buffer = new byte[8192];

        while (true)
        {
            var read = await stream.ReadOutputAsync(buffer, 0, buffer.Length, ct);
            if (read.EOF) break;

            var text = Encoding.UTF8.GetString(buffer, 0, read.Count);
            (read.Target == MultiplexedStream.TargetStream.StandardError ? stderr : stdout).Append(text);
        }

        return (stdout.ToString(), stderr.ToString());
    }

    /// <summary>Pull the human-readable text out of claude's `--output-format json` payload.</summary>
    private string ParseResult(string stdout, string stderr)
    {
        var trimmed = stdout.Trim();
        if (trimmed.Length == 0)
            return string.IsNullOrWhiteSpace(stderr) ? "(no output)" : stderr.Trim();

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.ValueKind == JsonValueKind.String)
                return result.GetString() ?? trimmed;
        }
        catch (JsonException)
        {
            // Not JSON (e.g. an early error before claude ran) — fall through to raw.
            logger.LogDebug("Run output was not JSON; returning raw stdout.");
        }

        return trimmed;
    }

    private static string TrimName(string containerName) =>
        containerName.StartsWith(NamePrefix, StringComparison.Ordinal)
            ? containerName[NamePrefix.Length..]
            : containerName;
}
