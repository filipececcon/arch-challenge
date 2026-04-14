namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

public class OutboxEventConfiguration : EntityConfiguration<OutboxEvent>
{
    public override void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("TB_OUTBOX_EVENT");

        base.Configure(builder);

        builder.Property(o => o.EventType)
            .HasColumnName("DS_EVENT_TYPE")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Payload)
            .HasColumnName("DS_PAYLOAD")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(o => o.Processed)
            .HasColumnName("ST_PROCESSED")
            .IsRequired();

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("DT_PROCESSED_AT");

        builder.Property(o => o.RetryCount)
            .HasColumnName("NR_RETRY_COUNT")
            .HasDefaultValue(0);

        // Índice composto para o polling do worker: filtra por ST_PROCESSED=false
        // e ordena por DT_CREATED_AT — evita full-table scan na tabela de outbox.
        builder.HasIndex(o => new { o.Processed, o.CreatedAt })
            .HasDatabaseName("IX_OUTBOX_EVENT_PROCESSED_CREATED");
    }
}


