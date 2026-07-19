using Ayoos.Application.Common.Interfaces;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Ayoos.Infrastructure.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ayoos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' was not configured.");

        services.AddDbContext<AyoosDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddDbContext<TenantStoreDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IPracticeRepository, PracticeRepository>();
        services.AddScoped<IPracticeProvisioner, PracticeProvisioner>();
        services.AddScoped<ITenantRegistry, TenantRegistry>();

        services.AddMultiTenant<TenantInfo>()
            .WithHeaderStrategy("X-Tenant")
            .WithEFCoreStore<TenantStoreDbContext, TenantInfo>();

        return services;
    }
}
