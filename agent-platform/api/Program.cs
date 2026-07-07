using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IAgentRunner, ClaudeAgentRunner>();
builder.Services.AddHostedService<JobDispatcher>();

var app = builder.Build();

// Apply migrations on startup — fine for a single-node homelab deployment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    db.Database.Migrate();
}

app.MapPost("/jobs", async (CreateJobRequest request, AgentDbContext db) =>
{
    var job = new AgentJob
    {
        Prompt = request.Prompt,
        RepoUrl = request.RepoUrl,
        Branch = request.Branch
    };

    db.Jobs.Add(job);
    await db.SaveChangesAsync();

    return Results.Created($"/jobs/{job.Id}", job);
});

app.MapGet("/jobs/{id:guid}", async (Guid id, AgentDbContext db) =>
    await db.Jobs.FindAsync(id) is { } job ? Results.Ok(job) : Results.NotFound());

app.MapGet("/jobs", async (AgentDbContext db) =>
    await db.Jobs.OrderByDescending(j => j.CreatedAt).Take(50).ToListAsync());

app.Run();

record CreateJobRequest(string Prompt, string? RepoUrl, string? Branch);
