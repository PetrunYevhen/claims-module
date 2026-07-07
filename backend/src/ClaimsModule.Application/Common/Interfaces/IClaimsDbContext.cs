using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Application-owned persistence abstraction (Clean Architecture: Application must not reference
/// Persistence directly). ClaimsDbContext implements this in the Persistence layer.
/// SaveChangesAsync doubles as the Unit of Work - a single call commits every change tracked
/// across all DbSets for the current MediatR request.
/// </summary>
public interface IClaimsDbContext
{
    DbSet<Claim> Claims { get; }
    DbSet<LossEvent> LossEvents { get; }
    DbSet<ClaimParty> ClaimParties { get; }
    DbSet<ClaimRiskObject> ClaimRiskObjects { get; }
    DbSet<ClaimReserveComponent> ClaimReserveComponents { get; }
    DbSet<ReserveHistory> ReserveHistory { get; }
    DbSet<ClaimDocument> ClaimDocuments { get; }
    DbSet<ClaimAuditLog> ClaimAuditLog { get; }
    DbSet<CauseOfLossCode> CauseOfLossCodes { get; }
    DbSet<Policy> Policies { get; }
    DbSet<ClaimNumberCounter> ClaimNumberCounters { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
