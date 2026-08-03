using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Common.Exceptions;
using Ayoos.Domain.Bookings;
using Ayoos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ayoos.Infrastructure.Repositories;

internal sealed class BookingRepository(AyoosDbContext dbContext)
    : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Bookings.SingleOrDefaultAsync(
            booking => booking.Id == id,
            cancellationToken);

    public async Task<BookingPage> ListAsync(
        Guid? providerId,
        Guid? patientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        BookingStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(
            dbContext.Bookings,
            providerId,
            patientId,
            fromDate,
            toDate,
            status);
        var totalCount = await query.CountAsync(cancellationToken);
        var bookings = await query
            .OrderBy(booking => booking.StartTime)
            .ThenBy(booking => booking.ProviderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new BookingPage(bookings, totalCount);
    }

    public async Task<IReadOnlyList<Booking>> GetProviderScheduleAsync(
        Guid providerId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        await Filter(
                dbContext.Bookings,
                providerId,
                null,
                fromDate,
                toDate,
                null)
            .OrderBy(booking => booking.StartTime)
            .ToListAsync(cancellationToken);

    public Task<bool> HasOverlapAsync(
        Guid providerId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? excludeBookingId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Bookings.AnyAsync(
            booking =>
                booking.ProviderId == providerId &&
                booking.Status != BookingStatus.Cancelled &&
                booking.StartTime < endTime &&
                booking.EndTime > startTime &&
                (!excludeBookingId.HasValue || booking.Id != excludeBookingId.Value),
            cancellationToken);

    public async Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation
            })
        {
            throw new ConflictException(
                "The provider already has a booking that overlaps this time.");
        }
    }

    private static IQueryable<Booking> Filter(
        IQueryable<Booking> query,
        Guid? providerId,
        Guid? patientId,
        DateOnly? fromDate,
        DateOnly? toDate,
        BookingStatus? status)
    {
        if (providerId.HasValue)
        {
            query = query.Where(booking => booking.ProviderId == providerId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(booking => booking.PatientId == patientId.Value);
        }

        if (fromDate.HasValue)
        {
            var from = new DateTimeOffset(
                fromDate.Value.ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(booking => booking.StartTime >= from);
        }

        if (toDate.HasValue)
        {
            var through = new DateTimeOffset(
                toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(booking => booking.StartTime < through);
        }

        if (status.HasValue)
        {
            query = query.Where(booking => booking.Status == status.Value);
        }

        return query;
    }
}
