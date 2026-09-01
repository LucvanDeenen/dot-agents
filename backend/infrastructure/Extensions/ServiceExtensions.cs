using AgentPlatform.Application.Agents;
using AgentPlatform.Infrastructure.Agents;
using AgentPlatform.Infrastructure.Options;
using Docker.DotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentPlatform.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RunnerOptions>()
            .Bind(configuration.GetSection(RunnerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One Docker client for the process, pointed at the local engine
        // (npipe on Windows, unix socket on Linux) by default.
        services.AddSingleton<IDockerClient>(_ => new DockerClientConfiguration().CreateClient());

        services.AddScoped<IAgentRunner, DockerAgentRunner>();

        // Create the dedicated agent network when the backend starts.
        services.AddHostedService<AgentNetworkInitializer>();
    }
}