namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Contexts;

public class CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Outbox> Outboxes => Set<Outbox>();
    
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
