using ArchChallenge.CashFlow.Domain.Entities;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

public class TransactionConfiguration : EntityConfiguration<Transaction>
{
    public override void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("TB_TRANSACTION");

        base.Configure(builder);

        builder
            .Property(t => t.Type)
            .HasColumnName("ST_TYPE")
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(t => t.Amount)
            .HasColumnName("VL_AMOUNT")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder
            .Property(t => t.Description)
            .HasColumnName("DS_TRANSACTION")
            .HasMaxLength(255);
    }
}
