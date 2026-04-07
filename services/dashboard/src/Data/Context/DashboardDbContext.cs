using ArchChallenge.Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchChallenge.Dashboard.Data.Context;

public class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<DailyConsolidation> DailyConsolidations => Set<DailyConsolidation>();
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dashboard");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DashboardDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
