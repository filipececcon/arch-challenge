namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

public sealed class OutboxEventConfiguration : OutboxEventConfigurationBase<OutboxEvent>
{
    public override void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("TB_OUTBOX_EVENT", schema: "outbox");

        base.Configure(builder);

        // Índice composto para polling do worker: filtra por ST_PROCESSED=false
        // e ordena por DT_CREATED_AT — evita full-table scan na tabela de outbox.
        builder.HasIndex(o => new { o.Processed, o.CreatedAt })
            .HasDatabaseName("IX_OUTBOX_EVENT_PROCESSED_CREATED");
    }
}


