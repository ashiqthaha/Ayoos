using Ayoos.Domain.Practices;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AyoosDbContext).Assembly);
        modelBuilder.Entity<Practice>().IsMultiTenant();
    }
}
