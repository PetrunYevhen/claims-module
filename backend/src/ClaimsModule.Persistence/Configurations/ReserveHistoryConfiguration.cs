using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ReserveHistoryConfiguration : IEntityTypeConfiguration<ReserveHistory>
{
    public void Configure(EntityTypeBuilder<ReserveHistory> builder)
    {
        builder.ToTable("ReserveHistory");
        builder.HasKey(x => x.ReserveHistoryId);
        builder.Property(x => x.ReserveHistoryId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.PostingJobId).HasMaxLength(100);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.IdempotencyKey);
        builder.HasIndex(x => x.ClaimId);
        builder.HasIndex(x => x.ReserveComponentId);
    }
}
