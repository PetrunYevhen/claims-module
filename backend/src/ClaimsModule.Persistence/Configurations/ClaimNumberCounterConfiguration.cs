using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimNumberCounterConfiguration : IEntityTypeConfiguration<ClaimNumberCounter>
{
    public void Configure(EntityTypeBuilder<ClaimNumberCounter> builder)
    {
        builder.ToTable("ClaimNumberCounters");
        builder.HasKey(x => x.Year);
        builder.Property(x => x.Year).ValueGeneratedNever();
    }
}
