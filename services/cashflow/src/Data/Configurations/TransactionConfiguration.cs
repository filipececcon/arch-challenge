using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchChallenge.CashFlow.Data.Configurations;

public class TransactionConfiguration : EntityConfiguration<Transaction>
{
    public override void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("TB_TRANSACTION");

        base.Configure(builder);

        builder
            .Property(t => t.Type)
            .HasColumnName("ST_TYPE")
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => (TransactionType)Enum.Parse(typeof(TransactionType), v, true))
            .HasMaxLength(10)
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
