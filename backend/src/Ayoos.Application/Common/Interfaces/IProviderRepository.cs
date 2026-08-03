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

    Task<AvailabilitySchedule?> GetAvailabilityScheduleAsync(
        Guid providerId,
        Guid availabilityId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAvailabilityOverlapAsync(
        Guid providerId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeAvailabilityId = null,
        CancellationToken cancellationToken = default);

    Task AddAvailabilityScheduleAsync(
        AvailabilitySchedule schedule,
        CancellationToken cancellationToken = default);

    Task AddAvailabilityExceptionAsync(
        AvailabilityException exception,
        CancellationToken cancellationToken = default);

    void RemoveAvailabilityException(AvailabilityException exception);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
