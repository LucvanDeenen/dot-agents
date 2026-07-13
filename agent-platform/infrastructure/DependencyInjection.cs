using AgentPlatform.Application.Abstractions;
using AgentPlatform.Infrastructure.Data;
using AgentPlatform.Infrastructure.Listeners;
using AgentPlatform.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AgentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AgentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IAgentDbContext>(sp => sp.GetRequiredService<AgentDbContext>());

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations();
        services.AddSingleton<RabbitMqConnectionHolder>();
        services.AddSingleton<ITaskPublisher, RabbitMqTaskPublisher>();

        // Order matters: the topology (exchange/queue/binding + the shared connection)
        // must exist before the listener tries to consume from it.
        services.AddHostedService<RabbitMqTopologyInitializer>();
        services.AddHostedService<TaskQueueListener>();

        return services;
    }
}
