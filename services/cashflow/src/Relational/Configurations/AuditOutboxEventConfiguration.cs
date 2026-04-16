namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Configurations;

public sealed class AuditOutboxEventConfiguration : OutboxEventConfigurationBase<AuditEvent>
{
    public override void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("TB_OUTBOX_AUDIT_EVENT", schema: "outbox");

        base.Configure(builder);

        builder.HasIndex(o => new { o.Processed, o.CreatedAt })
            .HasDatabaseName("IX_AUDIT_OUTBOX_EVENT_PROCESSED_CREATED");
    }
}
