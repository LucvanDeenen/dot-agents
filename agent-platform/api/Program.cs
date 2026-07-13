using AgentPlatform.Api.ExceptionHandling;
using AgentPlatform.Application;
using AgentPlatform.Infrastructure;
using AgentPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<TaskPublishFailedExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations on startup — fine for a single-node homelab deployment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
