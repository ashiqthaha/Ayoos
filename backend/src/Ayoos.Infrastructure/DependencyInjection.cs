using Ayoos.Application.Common.Interfaces;
using Ayoos.Infrastructure.Identity;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Ayoos.Infrastructure.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk;
using Microsoft.AspNetCore.Http;
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
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICurrentPracticeContext, CurrentPracticeContext>();
        services.AddScoped<IPracticeProvisioner, PracticeProvisioner>();
        services.AddScoped<ITenantRegistry, TenantRegistry>();

        services.AddKeycloakAdminHttpClient(new KeycloakAdminClientOptions
        {
            AuthServerUrl = configuration["Keycloak:AdminBaseUrl"]
                ?? throw new InvalidOperationException(
                    "Configuration value 'Keycloak:AdminBaseUrl' was not configured."),
            Realm = configuration["Keycloak:Realm"]
                ?? throw new InvalidOperationException(
                    "Configuration value 'Keycloak:Realm' was not configured."),
            Resource = configuration["Keycloak:AdminClientId"]
                ?? throw new InvalidOperationException(
                    "Configuration value 'Keycloak:AdminClientId' was not configured."),
            Credentials = new KeycloakClientInstallationCredentials
            {
                Secret = configuration["Keycloak:AdminClientSecret"]
                    ?? throw new InvalidOperationException(
                        "Configuration value 'Keycloak:AdminClientSecret' was not configured.")
            }
        });
        services.AddHttpClient();
        services.AddSingleton<KeycloakAdminTokenProvider>();
        services.AddScoped<IUserManagementService, KeycloakUserManagementService>();

        services.AddMultiTenant<TenantInfo>()
            .WithDelegateStrategy<HttpContext, TenantInfo>(context =>
            {
                var tenantClaim = context.User.FindFirst("practice")?.Value
                    ?? context.User.FindFirst("tenant")?.Value;

                return Task.FromResult<string?>(
                    string.IsNullOrWhiteSpace(tenantClaim)
                        ? null
                        : tenantClaim);
            })
            .WithHeaderStrategy("X-Tenant")
            .WithEFCoreStore<TenantStoreDbContext, TenantInfo>();

        return services;
    }
}
