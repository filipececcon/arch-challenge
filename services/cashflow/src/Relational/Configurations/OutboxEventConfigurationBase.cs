namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

/// <summary>
/// Configuração EF Core base para entidades de outbox transacional
/// (<see cref="OutboxEvent"/> e <see cref="AuditEvent"/>).
///
/// Centraliza o mapeamento de colunas comuns; subclasses apenas definem
/// o nome da tabela, do índice e quaisquer regras específicas.
/// </summary>
public abstract class OutboxEventConfigurationBase<TEvent> : EntityConfiguration<TEvent> where TEvent : EventBase
{
    public override void Configure(EntityTypeBuilder<TEvent> builder)
    {
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
    }
}
