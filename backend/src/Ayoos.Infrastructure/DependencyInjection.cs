using Ayoos.Application.Common.Interfaces;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Finbuckle.MultiTenant.Abstractions;
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
        var connectionString = configuration.GetConnectionString("Ayoos")
            ?? throw new InvalidOperationException(
                "Connection string 'Ayoos' was not configured.");

        services.AddDbContext<AyoosDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        // Placeholder tenant store; tenant resolution strategies are added with product features.
        services.AddMultiTenant<TenantInfo>()
            .WithInMemoryStore();

        return services;
    }
}
