using System.Text.Json;
using AgentPlatform.Api.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Route("jobs/events")]
public sealed class JobEventsController(JobStatusBroadcaster broadcaster) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        using var subscription = broadcaster.Subscribe(cancellationToken);

        await foreach (var update in subscription.Reader.ReadAllAsync(cancellationToken))
        {
            var payload = JsonSerializer.Serialize(update, JsonOptions);
            await Response.WriteAsync("event: job-status\n", cancellationToken);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}