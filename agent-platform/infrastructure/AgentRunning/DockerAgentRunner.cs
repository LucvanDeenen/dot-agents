using System.Text;
using System.Text.Json;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentPlatform.Infrastructure.AgentRunning;

// Runs one agent-runner container per task: the container gets the task +
// agent definition as a base64 JSON payload and the Claude subscription token,
// runs a Claude Code guide session (which delegates to a generated completer
// subagent), and its exit code / stdout become the task outcome.
public class DockerAgentRunner : IAgentRunner, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AgentRunnerOptions _options;
    private readonly ILogger<DockerAgentRunner> _logger;
    private readonly DockerClient _client;
    private readonly SemaphoreSlim _slots;

    public DockerAgentRunner(IOptions<AgentRunnerOptions> options, ILogger<DockerAgentRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = (string.IsNullOrWhiteSpace(_options.DockerEndpoint)
                ? new DockerClientConfiguration()
                : new DockerClientConfiguration(new Uri(_options.DockerEndpoint)))
            .CreateClient();
        _slots = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrency));
    }

    public async Task<AgentRunResult> RunAsync(AgentTask task, Agent agent, CancellationToken cancellationToken = default)
    {
        await _slots.WaitAsync(cancellationToken);
        try
        {
            return await RunContainerAsync(task, agent, cancellationToken);
        }
        catch (Exception ex) when (ex is DockerApiException or HttpRequestException or TimeoutException or IOException)
        {
            _logger.LogError(ex, "Docker run for task {TaskId} failed", task.Id);
            return new AgentRunResult(false,
                $"Agent container could not be run: {ex.Message} " +
                $"(is Docker running and the '{_options.Image}' image built?)");
        }
        finally
        {
            _slots.Release();
        }
    }

    private async Task<AgentRunResult> RunContainerAsync(AgentTask task, Agent agent, CancellationToken cancellationToken)
    {
        var runConfig = new
        {
            taskId = task.Id,
            instruction = task.Instruction,
            repoUrl = task.RepoUrl,
            branch = task.Branch,
            agentName = agent.Name,
            systemPrompt = agent.SystemPrompt,
            allowedTools = agent.AllowedTools,
            skills = agent.Skills.Select(s => new { s.Name, s.Description, s.Instructions })
        };
        var payload = Convert.ToBase64String(
            JsonSerializer.SerializeToUtf8Bytes(runConfig, JsonOptions));

        var env = new List<string>
        {
            $"RUN_CONFIG={payload}"
        };
        if (!string.IsNullOrWhiteSpace(_options.ClaudeCodeOAuthToken))
            env.Add($"CLAUDE_CODE_OAUTH_TOKEN={_options.ClaudeCodeOAuthToken}");
        else
            _logger.LogWarning("No Claude subscription token configured (AgentRunner:ClaudeCodeOAuthToken / CLAUDE_CODE_OAUTH_TOKEN); the run will fail unless the image carries its own auth");

        await EnsureImageAsync(cancellationToken);

        var container = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = _options.Image,
            Env = env,
            Labels = new Dictionary<string, string>
            {
                ["agent-platform.task-id"] = task.Id.ToString(),
                ["agent-platform.agent"] = agent.Name
            }
        }, cancellationToken);

        _logger.LogInformation("Starting agent container {ContainerId} for task {TaskId} (agent {Agent})",
            container.ID[..12], task.Id, agent.Name);

        try
        {
            await _client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), cancellationToken);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(_options.RunTimeoutMinutes));

            long exitCode;
            try
            {
                var wait = await _client.Containers.WaitContainerAsync(container.ID, timeout.Token);
                exitCode = wait.StatusCode;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await _client.Containers.KillContainerAsync(container.ID, new ContainerKillParameters(), CancellationToken.None);
                return new AgentRunResult(false, $"Agent run exceeded the {_options.RunTimeoutMinutes} minute timeout and was killed.");
            }

            var (stdout, stderr) = await ReadLogsAsync(container.ID, cancellationToken);
            var output = exitCode == 0 ? stdout : JoinNonEmpty(stdout, stderr);
            if (string.IsNullOrWhiteSpace(output))
                output = $"Agent container exited with code {exitCode} and produced no output.";

            return new AgentRunResult(exitCode == 0, Tail(output.Trim(), 16000));
        }
        finally
        {
            try
            {
                await _client.Containers.RemoveContainerAsync(container.ID,
                    new ContainerRemoveParameters { Force = true }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove agent container {ContainerId}", container.ID[..12]);
            }
        }
    }

    // Locally the image is built by hand (agent-runner:local); on a deployed
    // host it's a registry reference — pull it on first use so a fresh box
    // needs no manual `docker pull`.
    private async Task EnsureImageAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.Images.InspectImageAsync(_options.Image, cancellationToken);
            return;
        }
        catch (DockerImageNotFoundException)
        {
            // Fall through to pull.
        }

        var separator = _options.Image.LastIndexOf(':');
        var hasTag = separator > _options.Image.LastIndexOf('/');
        _logger.LogInformation("Agent image {Image} not present locally, pulling", _options.Image);
        await _client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = hasTag ? _options.Image[..separator] : _options.Image,
                Tag = hasTag ? _options.Image[(separator + 1)..] : "latest"
            },
            null,
            new Progress<JSONMessage>(),
            cancellationToken);
    }

    private async Task<(string Stdout, string Stderr)> ReadLogsAsync(string containerId, CancellationToken cancellationToken)
    {
        using var stream = await _client.Containers.GetContainerLogsAsync(containerId, false,
            new ContainerLogsParameters { ShowStdout = true, ShowStderr = true }, cancellationToken);
        return await stream.ReadOutputToEndAsync(cancellationToken);
    }

    private static string JoinNonEmpty(string stdout, string stderr)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(stdout)) builder.AppendLine(stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr)) builder.AppendLine(stderr.Trim());
        return builder.ToString();
    }

    private static string Tail(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[^maxLength..];

    public void Dispose()
    {
        _client.Dispose();
        _slots.Dispose();
        GC.SuppressFinalize(this);
    }
}
