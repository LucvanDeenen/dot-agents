using AgentPlatform.Api.Common;
using AgentPlatform.Application.Extensions;
using AgentPlatform.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseExceptionHandler();

app.MapHealthChecks(Constants.HealthEndpoint);
app.MapControllers();

app.Run();
