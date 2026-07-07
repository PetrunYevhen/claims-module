using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimRiskObjectConfiguration : IEntityTypeConfiguration<ClaimRiskObject>
{
    public void Configure(EntityTypeBuilder<ClaimRiskObject> builder)
    {
        builder.ToTable("ClaimRiskObjects");
        builder.HasKey(x => x.ClaimRiskObjectId);
        builder.Property(x => x.ClaimRiskObjectId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.AssetDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DamageDescription).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AssetReference).HasMaxLength(255);

        builder.HasIndex(x => x.ClaimId);
    }
}
