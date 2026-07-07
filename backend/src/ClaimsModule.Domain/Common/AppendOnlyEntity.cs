namespace ClaimsModule.Domain.Common;

/// <summary>
/// Base for tables that are never UPDATEd or DELETEd (ReserveHistory, ClaimAuditLog) -
/// no soft-delete flags and no UpdatedAt, since rows are immutable once inserted.
/// </summary>
public abstract class AppendOnlyEntity
{
    public Guid OrganisationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
