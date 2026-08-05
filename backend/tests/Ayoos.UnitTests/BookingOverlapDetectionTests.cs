using Ayoos.Domain.Bookings;

namespace Ayoos.UnitTests;

public sealed class BookingOverlapDetectionTests
{
    [Fact]
    public void Active_overlap_detection_uses_half_open_intervals_and_provider_scope()
    {
        var providerId = Guid.NewGuid();
        var start = new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.Zero);
        var overlapping = Create(providerId, start, start.AddMinutes(30));
        var adjacent = Create(providerId, start.AddMinutes(30), start.AddMinutes(60));
        var otherProvider = Create(Guid.NewGuid(), start, start.AddMinutes(30));

        var conflicts = BookingOverlapDetector.FindActiveConflicts(
            [overlapping, adjacent, otherProvider],
            providerId,
            start.AddMinutes(15),
            start.AddMinutes(30));

        Assert.Single(conflicts);
        Assert.Equal(overlapping.Id, conflicts[0].Id);
    }

    [Fact]
    public void Cancelled_and_terminal_bookings_do_not_block_slots()
    {
        var providerId = Guid.NewGuid();
        var start = new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.Zero);
        var cancelled = Create(providerId, start, start.AddMinutes(30));
        cancelled.CancelByPatient();
        var completed = Create(providerId, start, start.AddMinutes(30));
        completed.Confirm();
        completed.Complete();

        Assert.Empty(BookingOverlapDetector.FindActiveConflicts(
            [cancelled, completed],
            providerId,
            start,
            start.AddMinutes(30)));
    }

    private static Booking Create(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end) =>
        Booking.Create(
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid(),
            providerId,
            Guid.NewGuid(),
            start,
            end,
            null,
            start.AddDays(-1));
}
