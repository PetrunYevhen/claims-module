using ClaimsModule.API.Middleware;
using ClaimsModule.Application.Common.Interfaces;

namespace ClaimsModule.API.Http;

public class HttpContextCorrelationIdProvider(IHttpContextAccessor accessor) : ICorrelationIdProvider
{
    public Guid CorrelationId =>
        accessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] is Guid id ? id : Guid.Empty;
}
