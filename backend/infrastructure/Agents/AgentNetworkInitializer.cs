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
/// refuses to start, since no agent run could succeed anyway. On shutdown, it
/// tears down the agent run containers and removes the network.
/// </summary>
public sealed class AgentNetworkInitializer(
    IDockerClient docker,
    IOptions<RunnerOptions> options,
    ILogger<AgentNetworkInitializer> logger) : IHostedService
{
    // Label stamped on every agent run container (see DockerAgentRunner), used
    // to find them for cleanup. Docker label filters use "key=value" form.
    private const string AgentLabelFilter = "agent-platform.network=agents";

    private readonly RunnerOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var name = _options.Network;

        try
        {
            // Sweep leftovers from a previous session that didn't shut down
            // gracefully (e.g. killed from an IDE) — StopAsync can't run then.
            await RemoveAgentContainersAsync(cancellationToken);

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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var name = _options.Network;
        logger.LogInformation("Agent platform shutting down — removing agent runs and network '{Network}'.", name);

        try
        {
            // Remove agent run containers first — a network can't be deleted
            // while containers are still attached to it.
            await RemoveAgentContainersAsync(cancellationToken);

            var network = (await docker.Networks.ListNetworksAsync(new NetworksListParameters(), cancellationToken))
                .FirstOrDefault(n => n.Name == name);
            if (network is not null)
            {
                await docker.Networks.DeleteNetworkAsync(network.ID, cancellationToken);
                logger.LogInformation("Removed agent network '{Network}'.", name);
            }
        }
        catch (Exception ex)
        {
            // Best-effort on shutdown — log and move on.
            logger.LogWarning(ex, "Cleanup of agent network '{Network}' and its containers failed.", name);
        }
    }

    /// <summary>Force-remove every agent run container (matched by label), running or exited.</summary>
    private async Task RemoveAgentContainersAsync(CancellationToken ct)
    {
        var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool> { [AgentLabelFilter] = true }
            }
        }, ct);

        foreach (var container in containers)
        {
            try
            {
                await docker.Containers.RemoveContainerAsync(container.ID,
                    new ContainerRemoveParameters { Force = true }, ct);
                logger.LogInformation("Removed agent container {Id}.", container.ID);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to remove agent container {Id}.", container.ID);
            }
        }
    }
}
