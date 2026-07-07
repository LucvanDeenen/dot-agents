using AgentPlatform.Api.Models;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace AgentPlatform.Api.Services;

/// <summary>
/// Launches one ephemeral container per job from the agent-runner image,
/// waits for it to exit, captures its logs, then removes it.
/// Talks to Docker only through the scoped docker-socket-proxy — never
/// mounts docker.sock directly into this process.
/// </summary>
public class ClaudeAgentRunner : IAgentRunner
{
    private readonly DockerClient _docker;
    private readonly IConfiguration _config;
    private readonly ILogger<ClaudeAgentRunner> _logger;

    public ClaudeAgentRunner(IConfiguration config, ILogger<ClaudeAgentRunner> logger)
    {
        _config = config;
        _logger = logger;
        var endpoint = config["Docker:ProxyEndpoint"] ?? "tcp://docker-socket-proxy:2375";
        _docker = new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }

    public async Task<AgentRunResult> RunAsync(AgentJob job, CancellationToken cancellationToken)
    {
        var image = _config["Claude:AgentRunnerImage"] ?? "agent-runner:latest";
        var credentialsVolume = _config["Claude:CredentialsVolume"] ?? "claude-credentials";
        // Volume is expected to contain a single file, /credentials/token, holding
        // the CLAUDE_CODE_OAUTH_TOKEN value from `claude setup-token`.
        // entrypoint.sh reads it into the env var at container start rather than
        // it being set here, so it never appears in `docker inspect`.

        var createResponse = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = image,
            Env = new List<string>
            {
                $"PROMPT={job.Prompt}",
                $"REPO_URL={job.RepoUrl}",
                $"BRANCH={job.Branch}",
                // scope down what the agent can touch per invocation — tune per job type
                "ALLOWED_TOOLS=Read,Edit,Bash",
                "MAX_TURNS=15"
            },
            HostConfig = new HostConfig
            {
                AutoRemove = false, // remove manually after log capture, see below
                Mounts = new List<Mount>
                {
                    new()
                    {
                        Type = "volume",
                        Source = credentialsVolume,
                        Target = "/credentials",
                        ReadOnly = true
                    }
                },
                // TODO: CPU/memory limits — a 4GB Pi can't afford a runaway agent container
                Memory = 512L * 1024 * 1024,
                NanoCPUs = 1_000_000_000L // ~1 core
            },
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    ["agent-internal"] = new() // no edge access from inside the runner
                }
            }
        }, cancellationToken);

        var containerId = createResponse.ID;

        try
        {
            await _docker.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);

            var waitResponse = await _docker.Containers.WaitContainerAsync(containerId, cancellationToken);

            var logsStream = await _docker.Containers.GetContainerLogsAsync(
                containerId,
                new ContainerLogsParameters { ShowStdout = true, ShowStderr = true },
                cancellationToken);

            using var reader = new StreamReader(logsStream);
            var output = await reader.ReadToEndAsync();

            var success = waitResponse.StatusCode == 0;
            return new AgentRunResult(success, output, success ? null : $"Exit code {waitResponse.StatusCode}", containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent container {ContainerId} failed for job {JobId}", containerId, job.Id);
            return new AgentRunResult(false, null, ex.Message, containerId);
        }
        finally
        {
            try
            {
                await _docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove container {ContainerId}", containerId);
            }
        }
    }
}
