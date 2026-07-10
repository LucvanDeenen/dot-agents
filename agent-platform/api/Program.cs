using AgentPlatform.Api.Data;
using AgentPlatform.Api.Messaging;
using AgentPlatform.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IAgentRunner, ClaudeAgentRunner>();

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddSingleton<RabbitMqConnectionHolder>();
builder.Services.AddSingleton<ITaskPublisher, RabbitMqTaskPublisher>();

// Order matters: the topology (exchange/queue/binding + the shared connection)
// must exist before the listener tries to consume from it.
builder.Services.AddHostedService<RabbitMqTopologyInitializer>();
builder.Services.AddHostedService<TaskQueueListener>();

builder.Services.AddControllers();

// Drop JobDispatcher (the polling version) once TaskQueueListener is doing this job.
// builder.Services.AddHostedService<JobDispatcher>();

var app = builder.Build();

// Apply migrations on startup — fine for a single-node homelab deployment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.Run();