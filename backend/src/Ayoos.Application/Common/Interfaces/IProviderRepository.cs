using Ayoos.Domain.Providers;

namespace Ayoos.Application.Common.Interfaces;

public interface IProviderRepository
{
    Task<IReadOnlyList<Provider>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<Provider?> GetByIdAsync(
        Guid id,
        bool includeAvailability = false,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Provider provider,
        CancellationToken cancellationToken = default);

    void RemoveAvailabilityRules(IEnumerable<AvailabilityRule> rules);

    Task AddAvailabilityRulesAsync(
        IEnumerable<AvailabilityRule> rules,
        CancellationToken cancellationToken = default);

    Task AddAvailabilityExceptionAsync(
        AvailabilityException exception,
        CancellationToken cancellationToken = default);

    void RemoveAvailabilityException(AvailabilityException exception);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
