using AgentPlatform.Application.Jobs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.ExceptionHandling;

// Turns a TaskPublishFailedException from the CreateJob handler into the 202
// response the client should see — persisted but not queued, retry later.
public sealed class TaskPublishFailedExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not TaskPublishFailedException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status202Accepted;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status202Accepted,
                Title = "Task saved but not queued",
                Detail = "The task was persisted but could not be published to the task queue. It will need to be retried."
            }
        });
    }
}
