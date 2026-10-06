using backend.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace backend.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            DuplicateEventTypeException => StatusCodes.Status409Conflict,
            EventTypeLimitReachedException => StatusCodes.Status409Conflict,
            TenantIdNotFoundException => StatusCodes.Status404NotFound,
            UnknownEventTypeException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
            
        };
        
        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled Exception");
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = status,
            Title = status == StatusCodes.Status500InternalServerError ? "Something went wrong" : exception.Message
        }, cancellationToken);
        return true;
    }
};
