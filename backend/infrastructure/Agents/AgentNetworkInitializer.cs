using AgentPlatform.Infrastructure.Options;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentPlatform.Infrastructure.Agents;

/// <summary>
/// On backend startup, ensures the dedicated Docker network that all agent runs
/// attach to exists (see <see cref="RunnerOptions.Network"/>). Idempotent: it
/// creates the network only if missing. Fails fast — if the Docker engine is
/// unreachable or the network cannot be created, it throws and the application
/// refuses to start, since no agent run could succeed anyway.
/// </summary>
public sealed class AgentNetworkInitializer(
    IDockerClient docker,
    IOptions<RunnerOptions> options,
    ILogger<AgentNetworkInitializer> logger) : IHostedService
{
    private readonly RunnerOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var name = _options.Network;

        try
        {
            var existing = await docker.Networks.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            if (existing.Any(n => n.Name == name))
            {
                logger.LogInformation("Agent network '{Network}' already exists.", name);
                return;
            }

            var created = await docker.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = name,
                Driver = "bridge",
                Labels = new Dictionary<string, string>
                {
                    // Group the network under the agent-platform stack in Docker Desktop.
                    ["com.docker.compose.project"] = _options.ComposeProject,
                    ["agent-platform.network"] = "agents"
                }
            }, cancellationToken);

            logger.LogInformation("Created agent network '{Network}' ({Id}).", name, created.ID);
        }
        catch (Exception ex)
        {
            // Fail fast: without Docker + this network, no agent run can start.
            logger.LogCritical(ex, "Could not connect to Docker or create agent network '{Network}'. Aborting startup.", name);
            throw new InvalidOperationException(
                $"Agent platform cannot start: failed to connect to Docker and ensure the agent network '{name}'. " +
                "Is the Docker engine running?", ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
