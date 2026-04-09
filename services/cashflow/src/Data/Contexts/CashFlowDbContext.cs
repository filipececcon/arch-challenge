using ArchChallenge.CashFlow.Domain.Entities;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Contexts;

public class CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : DbContext(options)
{
    public DbSet<Transaction>  Transactions => Set<Transaction>();

    /// <summary>
    /// Tabela do Transactional Outbox Pattern.
    /// Eventos são inseridos aqui na mesma transação que a entidade principal,
    /// garantindo atomicidade. O <c>OutboxWorkerService</c> os sincroniza
    /// com o MongoDB de forma assíncrona.
    /// </summary>
    public DbSet<OutboxEvent>  OutboxEvents  => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var modifiedEntities = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntities)
            entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        
        return base.SaveChangesAsync(cancellationToken);
    }
    
}
