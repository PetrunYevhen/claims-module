using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimReserveComponentConfiguration : IEntityTypeConfiguration<ClaimReserveComponent>
{
    public void Configure(EntityTypeBuilder<ClaimReserveComponent> builder)
    {
        builder.ToTable("ClaimReserveComponents");
        builder.HasKey(x => x.ReserveComponentId);
        builder.Property(x => x.ReserveComponentId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.Notes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RowVer).IsRowVersion();

        builder.HasMany(x => x.History)
            .WithOne(h => h.ReserveComponent)
            .HasForeignKey(h => h.ReserveComponentId);

        builder.HasIndex(x => x.ClaimId);
    }
}
