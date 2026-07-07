using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(x => x.PolicyId);

        builder.Property(x => x.PolicyNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ClientName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.CoverageTypes).HasMaxLength(255).IsRequired();

        builder.HasIndex(x => x.PolicyNumber).IsUnique();

        // Simulated policy dataset - see FRS 5.5. POL-2023-000099 is intentionally expired,
        // used to exercise the "loss date outside policy period" warning (BR-C-02).
        builder.HasData(
            new Policy { PolicyId = new Guid("b2000000-0000-0000-0000-000000000001"), PolicyNumber = "POL-2024-001001", ClientName = "Meridian Transport LLC", EffectiveDate = new DateOnly(2024, 1, 1), ExpirationDate = new DateOnly(2026, 12, 31), Status = PolicyStatus.Active, CoverageTypes = "Vehicle,Cargo" },
            new Policy { PolicyId = new Guid("b2000000-0000-0000-0000-000000000002"), PolicyNumber = "POL-2024-001002", ClientName = "Harborview Properties Inc", EffectiveDate = new DateOnly(2024, 6, 1), ExpirationDate = new DateOnly(2026, 5, 31), Status = PolicyStatus.Active, CoverageTypes = "Property,Liability" },
            new Policy { PolicyId = new Guid("b2000000-0000-0000-0000-000000000003"), PolicyNumber = "POL-2025-002001", ClientName = "Coastal Builders Group", EffectiveDate = new DateOnly(2025, 3, 1), ExpirationDate = new DateOnly(2027, 2, 28), Status = PolicyStatus.Active, CoverageTypes = "Property,Equipment" },
            new Policy { PolicyId = new Guid("b2000000-0000-0000-0000-000000000004"), PolicyNumber = "POL-2025-002002", ClientName = "Stanton Medical Group", EffectiveDate = new DateOnly(2025, 1, 1), ExpirationDate = new DateOnly(2026, 12, 31), Status = PolicyStatus.Active, CoverageTypes = "Liability,Vehicle" },
            new Policy { PolicyId = new Guid("b2000000-0000-0000-0000-000000000005"), PolicyNumber = "POL-2023-000099", ClientName = "Archived Corp", EffectiveDate = new DateOnly(2020, 1, 1), ExpirationDate = new DateOnly(2021, 12, 31), Status = PolicyStatus.Expired, CoverageTypes = "Property" }
        );
    }
}
