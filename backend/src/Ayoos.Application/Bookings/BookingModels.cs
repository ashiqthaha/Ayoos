using Ayoos.Application.Providers;
using Ayoos.Domain.Bookings;

namespace Ayoos.Application.Bookings;

public sealed record BookingModel(
    Guid Id,
    string TenantId,
    Guid PatientId,
    Guid ProviderId,
    Guid? AvailabilityScheduleId,
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd,
    BookingStatus Status,
    string? Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CancellationReason,
    uint RowVersion);

public sealed record BookingConflictModel(
    Guid Id,
    Guid PatientId,
    Guid ProviderId,
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd,
    BookingStatus Status);

public sealed record BookingConflictPreviewModel(
    bool HasConflicts,
    IReadOnlyList<BookingConflictModel> Conflicts);

public sealed record CreateBookingResult(
    BookingModel? Booking,
    BookingConflictPreviewModel ConflictPreview);

public sealed record PagedBookingListModel(
    IReadOnlyList<BookingModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProviderScheduleModel(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<BookingModel> Bookings,
    IReadOnlyList<AvailabilitySlotModel> OpenSlots);

internal static class BookingMappings
{
    public static BookingModel ToModel(this Booking booking) =>
        new(
            booking.Id,
            booking.TenantId,
            booking.PatientId,
            booking.ProviderId,
            booking.AvailabilityScheduleId,
            booking.ScheduledStart,
            booking.ScheduledEnd,
            booking.Status,
            booking.Reason,
            booking.CreatedAt,
            booking.UpdatedAt,
            booking.CancellationReason,
            booking.RowVersion);

    public static BookingConflictModel ToConflictModel(this Booking booking) =>
        new(
            booking.Id,
            booking.PatientId,
            booking.ProviderId,
            booking.ScheduledStart,
            booking.ScheduledEnd,
            booking.Status);

    public static BookingConflictPreviewModel ToConflictPreview(
        this IReadOnlyList<Booking> conflicts) =>
        new(
            conflicts.Count > 0,
            conflicts.Select(booking => booking.ToConflictModel()).ToArray());
}
