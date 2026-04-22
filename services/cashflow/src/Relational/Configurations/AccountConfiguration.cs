namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

public sealed class AccountConfiguration : EntityConfiguration<Account>
{
    public override void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("TB_ACCOUNT");

        base.Configure(builder);

        builder
            .Property(a => a.UserId)
            .HasColumnName("ID_USER")
            .HasMaxLength(450)
            .IsRequired();

        builder
            .HasIndex(a => a.UserId)
            .IsUnique()
            .HasDatabaseName("IX_TB_ACCOUNT_USER");

        builder
            .Property(a => a.Balance)
            .HasColumnName("VL_BALANCE")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        // Relacionamento 1-N com Transaction (filho do agregado Account).
        // O backing field _transactions é usado pelo EF Core para popular a coleção
        // ao carregar com Include, e para detectar novos filhos adicionados via AddTransaction.
        builder
            .HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Navigation(a => a.Transactions)
            .HasField("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
