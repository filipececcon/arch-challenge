using ArchChallenge.Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchChallenge.Dashboard.Data.Configurations;

public class DailyConsolidationConfiguration : IEntityTypeConfiguration<DailyConsolidation>
{
    public void Configure(EntityTypeBuilder<DailyConsolidation> builder)
    {
        builder.ToTable("daily_consolidations");
        builder.HasKey(x => x.Date);
        builder.Property(x => x.Date).HasColumnName("date");
        builder.Property(x => x.TotalCredits).HasPrecision(18, 2);
        builder.Property(x => x.TotalDebits).HasPrecision(18, 2);
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
