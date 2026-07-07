using ClaimsModule.Application.Common.Exceptions;

namespace ClaimsModule.API.Middleware;

/// <summary>Translates Application-layer exceptions into the FRS 10.4 error response shape.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, type, title, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                "ValidationError",
                "One or more validation errors occurred.",
                ve.Errors),
            BusinessRuleException be => (
                StatusCodes.Status422UnprocessableEntity,
                "BusinessRuleViolation",
                "One or more business rules were violated.",
                be.Errors),
            NotFoundException ne => (
                StatusCodes.Status404NotFound,
                "NotFound",
                ne.Message,
                new Dictionary<string, string[]>()),
            _ => (
                StatusCodes.Status500InternalServerError,
                "ServerError",
                "An unexpected error occurred.",
                new Dictionary<string, string[]>())
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] is Guid id ? id : (Guid?)null;

        await context.Response.WriteAsJsonAsync(new { type, title, status, errors, correlationId });
    }
}
