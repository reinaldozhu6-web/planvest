using Microsoft.AspNetCore.Diagnostics;

namespace PlanVest.Api.Infrastructure;

public sealed class BadHttpRequestExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException) return false;

        await Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request body",
            detail: "The request body contains malformed JSON or an invalid value.")
            .ExecuteAsync(httpContext);
        return true;
    }
}
