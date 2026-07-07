using System.Text.Json;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Common.Services;

public class AuditLogService(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    ICorrelationIdProvider correlationIdProvider) : IAuditLogService
{
    public Task LogAsync(
        Guid claimId,
        string eventType,
        string description,
        object? oldValue = null,
        object? newValue = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        context.ClaimAuditLog.Add(new ClaimAuditLog
        {
            AuditLogId = Guid.NewGuid(),
            ClaimId = claimId,
            OrganisationId = currentUser.OrganisationId,
            EventType = eventType,
            Description = description,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CorrelationId = correlationIdProvider.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = currentUser.UserId
        });

        // Not saved here - stays tracked so it commits atomically with the rest of the
        // caller's unit of work via the handler's own SaveChangesAsync.
        return Task.CompletedTask;
    }
}
