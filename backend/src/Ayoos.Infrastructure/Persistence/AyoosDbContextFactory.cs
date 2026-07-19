using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ayoos.Infrastructure.Persistence;

public sealed class AyoosDbContextFactory : IDesignTimeDbContextFactory<AyoosDbContext>
{
    public AyoosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AyoosDbContext>()
            .UseNpgsql(DesignTimeConnectionString.Get())
            .Options;

        var tenantInfo = new TenantInfo
        {
            Id = Guid.Empty.ToString("D"),
            Identifier = "design-time",
            Name = "Design Time"
        };

        return MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(
            tenantInfo,
            options);
    }
}

public sealed class TenantStoreDbContextFactory
    : IDesignTimeDbContextFactory<TenantStoreDbContext>
{
    public TenantStoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TenantStoreDbContext>()
            .UseNpgsql(DesignTimeConnectionString.Get())
            .Options;

        return new TenantStoreDbContext(options);
    }
}

internal static class DesignTimeConnectionString
{
    public static string Get() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? "Host=localhost;Port=5432;Database=ayoos;Username=postgres;Password=postgres";
}
