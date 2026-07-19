using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ayoos.Infrastructure.Persistence;

public static class MigrationExtensions
{
    public static async Task ApplyAyoosMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var tenantStore = scope.ServiceProvider.GetRequiredService<TenantStoreDbContext>();
        await tenantStore.Database.MigrateAsync(cancellationToken);

        var ayoosDbContext = scope.ServiceProvider.GetRequiredService<AyoosDbContext>();
        await ayoosDbContext.Database.MigrateAsync(cancellationToken);
    }
}
