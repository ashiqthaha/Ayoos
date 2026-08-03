using Ayoos.Domain.Bookings;

namespace Ayoos.Application.Bookings;

public sealed record BookingModel(
    Guid Id,
    string TenantId,
    Guid PatientId,
    Guid ProviderId,
    Guid? AvailabilityScheduleId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingStatus Status,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record PagedBookingListModel(
    IReadOnlyList<BookingModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

internal static class BookingMappings
{
    public static BookingModel ToModel(this Booking booking) =>
        new(
            booking.Id,
            booking.TenantId,
            booking.PatientId,
            booking.ProviderId,
            booking.AvailabilityScheduleId,
            booking.StartTime,
            booking.EndTime,
            booking.Status,
            booking.Reason,
            booking.CreatedAt);
}
