using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(x => x.ClaimId);
        builder.Property(x => x.ClaimId).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.ClaimNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PolicyNumber).HasMaxLength(50);
        builder.Property(x => x.ClientName).HasMaxLength(255);
        builder.Property(x => x.ClosureReason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.RowVer).IsRowVersion();

        builder.HasIndex(x => new { x.OrganisationId, x.ClaimNumber }).IsUnique();

        builder.HasMany(x => x.Parties).WithOne(p => p.Claim).HasForeignKey(p => p.ClaimId);
        builder.HasMany(x => x.RiskObjects).WithOne(r => r.Claim).HasForeignKey(r => r.ClaimId);
        builder.HasMany(x => x.ReserveComponents).WithOne(r => r.Claim).HasForeignKey(r => r.ClaimId);
        builder.HasMany(x => x.Documents).WithOne(d => d.Claim).HasForeignKey(d => d.ClaimId);
        builder.HasMany(x => x.AuditLogEntries).WithOne(a => a.Claim).HasForeignKey(a => a.ClaimId);
    }
}
