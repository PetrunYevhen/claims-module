namespace ClaimsModule.API.Middleware;

/// <summary>
/// Reads X-Correlation-Id from the incoming request (or generates one), stores it on
/// HttpContext.Items for the rest of the pipeline, and echoes it back in the response header.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header)
            && Guid.TryParse(header, out var parsed)
                ? parsed
                : Guid.NewGuid();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId.ToString();

        await next(context);
    }
}
