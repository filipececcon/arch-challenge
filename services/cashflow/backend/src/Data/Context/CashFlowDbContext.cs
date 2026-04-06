using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchChallenge.CashFlow.Data.Context;

public class CashFlowDbContext(DbContextOptions<CashFlowDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cashflow");
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
