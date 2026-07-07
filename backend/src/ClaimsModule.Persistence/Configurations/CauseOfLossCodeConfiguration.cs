using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class CauseOfLossCodeConfiguration : IEntityTypeConfiguration<CauseOfLossCode>
{
    public void Configure(EntityTypeBuilder<CauseOfLossCode> builder)
    {
        builder.ToTable("CauseOfLossCodes");
        builder.HasKey(x => x.CauseOfLossCodeId);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        // Seeded reference data - see FRS 5.6.
        builder.HasData(
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000001"), Code = "COL-FIRE", Name = "Fire", PerilCategory = PerilCategory.Property, IsActive = true, SortOrder = 1 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000002"), Code = "COL-FLOOD", Name = "Flood", PerilCategory = PerilCategory.Weather, IsActive = true, SortOrder = 2 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000003"), Code = "COL-THEFT", Name = "Theft", PerilCategory = PerilCategory.Crime, IsActive = true, SortOrder = 3 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000004"), Code = "COL-VEH-COL", Name = "Vehicle Collision", PerilCategory = PerilCategory.Auto, IsActive = true, SortOrder = 4 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000005"), Code = "COL-VEH-COMP", Name = "Vehicle Comprehensive", PerilCategory = PerilCategory.Auto, IsActive = true, SortOrder = 5 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000006"), Code = "COL-LIAB", Name = "Third Party Liability", PerilCategory = PerilCategory.Liability, IsActive = true, SortOrder = 6 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000007"), Code = "COL-EQUIP", Name = "Equipment Breakdown", PerilCategory = PerilCategory.Equipment, IsActive = true, SortOrder = 7 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000008"), Code = "COL-WIND", Name = "Wind / Storm", PerilCategory = PerilCategory.Weather, IsActive = true, SortOrder = 8 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000009"), Code = "COL-INJURY", Name = "Bodily Injury", PerilCategory = PerilCategory.Liability, IsActive = true, SortOrder = 9 },
            new CauseOfLossCode { CauseOfLossCodeId = new Guid("a1000000-0000-0000-0000-000000000010"), Code = "COL-OTHER", Name = "Other / Unknown", PerilCategory = PerilCategory.General, IsActive = true, SortOrder = 10 }
        );
    }
}
