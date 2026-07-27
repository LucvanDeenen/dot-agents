using Microsoft.Extensions.DependencyInjection;

namespace AgentPlatform.Application.Extensions;

public static class ServiceExtension
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceExtension).Assembly));
    }
}
