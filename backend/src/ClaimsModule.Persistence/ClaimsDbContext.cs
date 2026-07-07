using System.Reflection;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence;

public class ClaimsDbContext : DbContext, IClaimsDbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<LossEvent> LossEvents => Set<LossEvent>();
    public DbSet<ClaimParty> ClaimParties => Set<ClaimParty>();
    public DbSet<ClaimRiskObject> ClaimRiskObjects => Set<ClaimRiskObject>();
    public DbSet<ClaimReserveComponent> ClaimReserveComponents => Set<ClaimReserveComponent>();
    public DbSet<ReserveHistory> ReserveHistory => Set<ReserveHistory>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    public DbSet<ClaimAuditLog> ClaimAuditLog => Set<ClaimAuditLog>();
    public DbSet<CauseOfLossCode> CauseOfLossCodes => Set<CauseOfLossCode>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<ClaimNumberCounter> ClaimNumberCounters => Set<ClaimNumberCounter>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);

        configurationBuilder.Properties<ClaimStatus>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<ClaimSeverity>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<PartyRole>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<PartyType>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<AssetType>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<ReserveComponentType>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<ReserveComponentStatus>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<ReserveTransactionType>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<ReserveApprovalStatus>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<ReservePostingStatus>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<PolicyStatus>().HaveConversion<string>().HaveMaxLength(50);
        configurationBuilder.Properties<PerilCategory>().HaveConversion<string>().HaveMaxLength(50);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Soft-delete query filter applied uniformly to every AuditableEntity - see FRS 15.2.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                typeof(ClaimsDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(null, [modelBuilder]);
            }
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : AuditableEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// BR-A-01: ClaimAuditLog is append-only - no UPDATE or DELETE is ever performed, enforced here
    /// in code since a shared dev SQL login can't be restricted to INSERT/SELECT-only at the DB level.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var illegalChange = ChangeTracker.Entries<ClaimAuditLog>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (illegalChange)
        {
            throw new InvalidOperationException(
                "ClaimAuditLog is append-only (BR-A-01) - UPDATE and DELETE are not permitted.");
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
