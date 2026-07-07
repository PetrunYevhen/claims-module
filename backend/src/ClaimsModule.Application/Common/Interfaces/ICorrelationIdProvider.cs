namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>FRS 14.2: CorrelationId from the HTTP request must propagate to every audit entry in that request.</summary>
public interface ICorrelationIdProvider
{
    Guid CorrelationId { get; }
}
