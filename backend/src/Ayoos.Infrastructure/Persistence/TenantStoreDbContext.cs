using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Persistence;

public sealed class TenantStoreDbContext(
    DbContextOptions<TenantStoreDbContext> options)
    : EFCoreStoreDbContext<TenantInfo>(options);
