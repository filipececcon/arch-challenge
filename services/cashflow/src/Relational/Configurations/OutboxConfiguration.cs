namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

/// <summary>
/// Configuração EF Core para a entidade unificada <see cref="Outbox"/>.
/// Tabela única com coluna discriminadora <c>DS_TARGET</c> (<see cref="OutboxTarget"/>).
/// </summary>
public sealed class OutboxConfiguration : EntityConfiguration<Outbox>
{
    public override void Configure(EntityTypeBuilder<Outbox> builder)
    {
        builder.ToTable("TB_OUTBOX", schema: "outbox");

        base.Configure(builder);

        builder.Property(o => o.Kind)
            .HasColumnName("DS_KIND")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Payload)
            .HasColumnName("DS_PAYLOAD")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.Target)
            .HasColumnName("DS_TARGET")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Processed)
            .HasColumnName("ST_PROCESSED")
            .IsRequired();

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("DT_PROCESSED_AT");

        builder.Property(o => o.RetryCount)
            .HasColumnName("NR_RETRY_COUNT")
            .HasDefaultValue(0);

        // Índice composto: filtra por ST_PROCESSED=false + DS_TARGET e ordena por DT_CREATED_AT
        // — evita full-table scan na tabela de outbox independente do target.
        builder.HasIndex(o => new { o.Processed, o.Target, o.CreatedAt })
            .HasDatabaseName("IX_OUTBOX_PROCESSED_TARGET_CREATED");
    }
}

