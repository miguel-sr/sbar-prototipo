using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sbar.Domain.Exceptions;

namespace Sbar.Api.ErrorHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            DomainException dominioException when dominioException.StatusCode is not null => (int)dominioException.StatusCode,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        httpContext.Response.StatusCode = status;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = exception.Message,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier },
        }, cancellationToken);

        return true;
    }
}
