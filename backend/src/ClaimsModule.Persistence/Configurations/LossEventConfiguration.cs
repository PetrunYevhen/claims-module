using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class LossEventConfiguration : IEntityTypeConfiguration<LossEvent>
{
    public void Configure(EntityTypeBuilder<LossEvent> builder)
    {
        builder.ToTable("LossEvents");
        builder.HasKey(x => x.LossEventId);
        builder.Property(x => x.LossEventId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.LossDescription).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.LossLocation).HasMaxLength(500);
        builder.Property(x => x.CauseOfLossCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PoliceReportNumber).HasMaxLength(100);

        // One loss event per claim - see FRS 1.2.
        builder.HasOne(x => x.Claim)
            .WithOne(c => c.LossEvent)
            .HasForeignKey<LossEvent>(x => x.ClaimId);

        builder.HasIndex(x => x.ClaimId).IsUnique();
    }
}
