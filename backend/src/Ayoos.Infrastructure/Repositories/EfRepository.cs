using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Common;
using Ayoos.Infrastructure.Persistence;

namespace Ayoos.Infrastructure.Repositories;

internal sealed class EfRepository<TEntity>(AyoosDbContext dbContext)
    : IRepository<TEntity>
    where TEntity : Entity
{
    public async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }
}
