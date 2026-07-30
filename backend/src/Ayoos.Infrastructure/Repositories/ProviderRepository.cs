using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Repositories;

internal sealed class ProviderRepository(AyoosDbContext dbContext)
    : IProviderRepository
{
    public async Task<IReadOnlyList<Provider>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Providers
            .OrderBy(provider => provider.LastName)
            .ThenBy(provider => provider.FirstName)
            .ToListAsync(cancellationToken);

    public Task<Provider?> GetByIdAsync(
        Guid id,
        bool includeAvailability = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Provider> query = dbContext.Providers;

        if (includeAvailability)
        {
            query = query
                .Include(provider => provider.AvailabilityRules)
                .Include(provider => provider.AvailabilityExceptions);
        }

        return query.SingleOrDefaultAsync(provider => provider.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        Provider provider,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Providers.AddAsync(provider, cancellationToken);
    }

    public void RemoveAvailabilityRules(IEnumerable<AvailabilityRule> rules)
    {
        dbContext.AvailabilityRules.RemoveRange(rules);
    }

    public async Task AddAvailabilityRulesAsync(
        IEnumerable<AvailabilityRule> rules,
        CancellationToken cancellationToken = default)
    {
        await dbContext.AvailabilityRules.AddRangeAsync(rules, cancellationToken);
    }

    public async Task AddAvailabilityExceptionAsync(
        AvailabilityException exception,
        CancellationToken cancellationToken = default)
    {
        await dbContext.AvailabilityExceptions.AddAsync(exception, cancellationToken);
    }

    public void RemoveAvailabilityException(AvailabilityException exception)
    {
        dbContext.AvailabilityExceptions.Remove(exception);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
