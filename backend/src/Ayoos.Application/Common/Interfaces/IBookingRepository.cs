using Ayoos.Domain.Bookings;

namespace Ayoos.Application.Common.Interfaces;

public sealed record BookingPage(
    IReadOnlyList<Booking> Items,
    int TotalCount);

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BookingPage> ListAsync(
        Guid? providerId,
        Guid? patientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        BookingStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetProviderScheduleAsync(
        Guid providerId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> FindActiveOverlapsAsync(
        Guid providerId,
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd,
        Guid? excludeBookingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
