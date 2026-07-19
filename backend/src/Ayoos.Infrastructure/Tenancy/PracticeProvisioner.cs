using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Practices;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Tenancy;

internal sealed class PracticeProvisioner(
    IMultiTenantStore<TenantInfo> tenantStore,
    DbContextOptions<AyoosDbContext> dbContextOptions)
    : IPracticeProvisioner
{
    public async Task ProvisionAsync(
        Practice practice,
        CancellationToken cancellationToken = default)
    {
        var tenantInfo = new TenantInfo
        {
            Id = practice.Id.ToString("D"),
            Identifier = practice.Slug,
            Name = practice.Name
        };

        if (!await tenantStore.AddAsync(tenantInfo))
        {
            throw new ConflictException(
                $"A practice with slug '{practice.Slug}' already exists.");
        }

        try
        {
            await using var dbContext =
                MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(
                    tenantInfo,
                    dbContextOptions);

            await dbContext.Practices.AddAsync(practice, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await tenantStore.RemoveAsync(tenantInfo.Id);
            throw;
        }
    }
}
