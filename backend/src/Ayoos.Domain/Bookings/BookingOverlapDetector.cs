namespace Ayoos.Domain.Bookings;

public static class BookingOverlapDetector
{
    public static IReadOnlyList<Booking> FindActiveConflicts(
        IEnumerable<Booking> existingBookings,
        Guid providerId,
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd,
        Guid? excludeBookingId = null) =>
        existingBookings
            .Where(booking =>
                booking.ProviderId == providerId &&
                booking.Id != excludeBookingId &&
                booking.IsActive &&
                booking.ScheduledStart < scheduledEnd &&
                booking.ScheduledEnd > scheduledStart)
            .OrderBy(booking => booking.ScheduledStart)
            .ToArray();
}
