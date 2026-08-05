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

    Task<IReadOnlyList<AvailabilitySchedule>> ListActiveAvailabilitySchedulesAsync(
        Guid providerId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default);

    Task<AvailabilityException?> GetAvailabilityExceptionAsync(
        Guid providerId,
        Guid exceptionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilityException>> ListAvailabilityExceptionsAsync(
        Guid providerId,
        DateOnly fromDate,
        DateOnly toDate,
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
