using Ayoos.Domain.Practices;
using Ayoos.Domain.Patients;
using Ayoos.Domain.Providers;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Persistence;

public sealed class AyoosDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<AyoosDbContext> options)
    : MultiTenantDbContext(multiTenantContextAccessor, options)
{
    public DbSet<Practice> Practices => Set<Practice>();

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();

    public DbSet<AvailabilityException> AvailabilityExceptions =>
        Set<AvailabilityException>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AyoosDbContext).Assembly);
        modelBuilder.Entity<Practice>().IsMultiTenant();
        modelBuilder.Entity<Provider>().IsMultiTenant();
        modelBuilder.Entity<Patient>().IsMultiTenant();
        modelBuilder.Entity<EmergencyContact>().IsMultiTenant();
        modelBuilder.Entity<AvailabilityRule>().IsMultiTenant();
        modelBuilder.Entity<AvailabilityException>().IsMultiTenant();
    }
}
