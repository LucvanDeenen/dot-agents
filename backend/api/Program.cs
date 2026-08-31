using AgentPlatform.Application.Extensions;
using AgentPlatform.Common;
using AgentPlatform.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks(Constants.Endpoints.Health);
app.MapControllers();

app.Run();
