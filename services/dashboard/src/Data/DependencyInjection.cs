using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Data.Context;
using ArchChallenge.Dashboard.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArchChallenge.Dashboard.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DashboardDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDailyBalanceReadStore, DailyBalanceReadStore>();
        services.AddScoped<ITransactionRegisteredProcessor, TransactionRegisteredProcessor>();

        return services;
    }

    public static async Task MigrateAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        await db.Database.MigrateAsync();
    }
}
