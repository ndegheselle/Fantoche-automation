using Fantoche.Shared.Base;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fantoche.Supervisor.Api;

/// <summary>
/// Turn what the services throw into a problem the applications turn back into the same exception
/// (see <see cref="SupervisorErrors"/>), so a page handles a failure the same way in both modes.
/// </summary>
internal sealed class ErrorHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellation)
    {
        int status = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            ValidationException or PackageValidationException or ExecutionException or ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        ProblemDetails problem = new()
        {
            Status = status,
            Type = SupervisorErrors.TypeOf(exception),
            Detail = exception.Message,
        };
        if (exception is ValidationException validation)
            problem.Extensions[SupervisorErrors.ValidationErrors] = validation.Errors;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, FantocheJson.Options, cancellation);
        return true;
    }
}
