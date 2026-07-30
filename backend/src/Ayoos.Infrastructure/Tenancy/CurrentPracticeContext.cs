using Ayoos.Application.Common.Interfaces;
using Finbuckle.MultiTenant.Abstractions;

namespace Ayoos.Infrastructure.Tenancy;

internal sealed class CurrentPracticeContext(
    IMultiTenantContextAccessor<TenantInfo> multiTenantContextAccessor)
    : ICurrentPracticeContext
{
    public Guid PracticeId
    {
        get
        {
            var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;

            if (!Guid.TryParse(tenantId, out var practiceId))
            {
                throw new InvalidOperationException(
                    "A valid practice tenant is required for this operation.");
            }

            return practiceId;
        }
    }
}
