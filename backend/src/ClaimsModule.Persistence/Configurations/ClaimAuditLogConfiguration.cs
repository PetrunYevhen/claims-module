using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimAuditLogConfiguration : IEntityTypeConfiguration<ClaimAuditLog>
{
    public void Configure(EntityTypeBuilder<ClaimAuditLog> builder)
    {
        builder.ToTable("ClaimAuditLog");
        builder.HasKey(x => x.AuditLogId);
        builder.Property(x => x.AuditLogId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.OldValue).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValue).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RelatedEntityType).HasMaxLength(100);

        builder.HasIndex(x => x.ClaimId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
