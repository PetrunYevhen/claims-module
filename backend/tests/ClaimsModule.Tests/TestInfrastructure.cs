using AutoMapper;
using ClaimsModule.Application.Claims.Mapping;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reserves.Mapping;
using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClaimsModule.Tests;

/// <summary>
/// SQL Server generates ROWVERSION server-side; the InMemory provider does not and treats the
/// required RowVer column as a missing value on insert. This interceptor fills it for newly-added
/// entities so the production handlers/DbContext run against InMemory completely unchanged.
/// </summary>
public sealed class RowVersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Populate(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Populate(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Populate(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;

            var rowVer = entry.Metadata.FindProperty("RowVer");
            if (rowVer is not null && entry.CurrentValues["RowVer"] is null)
            {
                entry.CurrentValues["RowVer"] = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            }
        }
    }
}

/// <summary>Seeded policy ids/codes that ship with the DbContext HasData (FRS 5.5 / 5.6).</summary>
public static class Seeded
{
    public static readonly Guid MeridianPolicyId = new("b2000000-0000-0000-0000-000000000001");
    public static readonly Guid ExpiredPolicyId = new("b2000000-0000-0000-0000-000000000005");
    public const string FireCode = "COL-FIRE";
}

/// <summary>Spins up an isolated in-memory ClaimsDbContext with the HasData seed applied.</summary>
public static class TestDb
{
    public static ClaimsDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .AddInterceptors(new RowVersionInterceptor())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ClaimsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

public sealed class FakeCurrentUser(string role = "handler", Guid? userId = null) : ICurrentUserService
{
    public Guid OrganisationId => SeedConstants.OrganisationId;
    public Guid UserId { get; } = userId ?? new Guid("a1a1a1a1-0000-0000-0000-000000000001");
    public string Role { get; set; } = role;
}

/// <summary>Records the audit events a handler emits without needing the real audit pipeline.</summary>
public sealed class RecordingAuditLog : IAuditLogService
{
    public List<string> Events { get; } = [];

    public Task LogAsync(Guid claimId, string eventType, string description,
        object? oldValue = null, object? newValue = null, Guid? relatedEntityId = null,
        string? relatedEntityType = null, CancellationToken cancellationToken = default)
    {
        Events.Add(eventType);
        return Task.CompletedTask;
    }
}

public sealed class RecordingJobEnqueuer : IBackgroundJobEnqueuer
{
    public List<Guid> Enqueued { get; } = [];
    public void EnqueueGlPosting(Guid reserveHistoryId) => Enqueued.Add(reserveHistoryId);
}

public sealed class StubClaimNumberGenerator(string number = "CLM-2026-0000001") : IClaimNumberGenerator
{
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default) => Task.FromResult(number);
}

public static class MapperFactory
{
    public static IMapper Create()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ClaimsMappingProfile>();
            cfg.AddProfile<ReservesMappingProfile>();
        }, NullLoggerFactory.Instance);

        return config.CreateMapper();
    }
}

/// <summary>Builds a persisted Draft claim (with loss event + optional claimant) for handler tests.</summary>
public static class ClaimBuilder
{
    public static Claim AddClaim(this ClaimsDbContext context, Guid? policyId = null, bool withClaimant = true,
        ClaimsModule.Domain.Enums.ClaimStatus status = ClaimsModule.Domain.Enums.ClaimStatus.Open)
    {
        var now = DateTimeOffset.UtcNow;
        var claim = new Claim
        {
            ClaimId = Guid.NewGuid(),
            OrganisationId = SeedConstants.OrganisationId,
            ClaimNumber = $"CLM-2026-{Random.Shared.Next(1, 9_999_999):0000000}",
            PolicyId = policyId,
            Status = status,
            ReportedDate = now,
            CreatedAt = now
        };
        context.Claims.Add(claim);

        context.LossEvents.Add(new LossEvent
        {
            LossEventId = Guid.NewGuid(),
            ClaimId = claim.ClaimId,
            OrganisationId = SeedConstants.OrganisationId,
            LossDate = now.AddDays(-2),
            LossDescription = "A sufficiently long loss description for validation.",
            CauseOfLossCode = Seeded.FireCode,
            ReportDate = now,
            CreatedAt = now
        });

        if (withClaimant)
        {
            context.ClaimParties.Add(new ClaimParty
            {
                ClaimPartyId = Guid.NewGuid(),
                ClaimId = claim.ClaimId,
                OrganisationId = SeedConstants.OrganisationId,
                PartyRole = ClaimsModule.Domain.Enums.PartyRole.Claimant,
                PartyType = ClaimsModule.Domain.Enums.PartyType.Person,
                FirstName = "Test",
                LastName = "Claimant",
                IsActive = true,
                CreatedAt = now
            });
        }

        context.SaveChanges();
        return claim;
    }
}
