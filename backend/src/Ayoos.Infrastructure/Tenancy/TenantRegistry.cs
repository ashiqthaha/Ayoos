using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Finbuckle.MultiTenant.Abstractions;

namespace Ayoos.Infrastructure.Tenancy;

internal sealed class TenantRegistry(IMultiTenantStore<TenantInfo> tenantStore)
    : ITenantRegistry
{
    public async Task<bool> IdentifierExistsAsync(
        string identifier,
        CancellationToken cancellationToken = default) =>
        await tenantStore.GetByIdentifierAsync(identifier) is not null;

    public async Task UpdateAsync(
        Guid practiceId,
        string identifier,
        string name,
        CancellationToken cancellationToken = default)
    {
        var tenantInfo = await tenantStore.GetAsync(practiceId.ToString("D"));
        if (tenantInfo is null)
        {
            throw new NotFoundException(
                $"Tenant registration for practice '{practiceId}' was not found.");
        }

        tenantInfo = new TenantInfo
        {
            Id = tenantInfo.Id,
            Identifier = identifier,
            Name = name
        };

        if (!await tenantStore.UpdateAsync(tenantInfo))
        {
            throw new ConflictException(
                $"Tenant registration for practice '{practiceId}' could not be updated.");
        }
    }
}
