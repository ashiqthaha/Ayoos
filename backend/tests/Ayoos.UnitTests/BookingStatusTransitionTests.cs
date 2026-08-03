using Ayoos.Domain.Bookings;

namespace Ayoos.UnitTests;

public sealed class BookingStatusTransitionTests
{
    [Fact]
    public void Requested_can_be_confirmed_then_completed()
    {
        var booking = CreateBooking();

        booking.Confirm();
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        booking.Complete();
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Throws<InvalidOperationException>(booking.Cancel);
        Assert.Throws<InvalidOperationException>(booking.MarkNoShow);
    }

    [Fact]
    public void Requested_or_confirmed_can_be_cancelled_and_cancelled_is_terminal()
    {
        var requested = CreateBooking();
        requested.Cancel();
        Assert.Equal(BookingStatus.Cancelled, requested.Status);
        Assert.Throws<InvalidOperationException>(requested.Confirm);

        var confirmed = CreateBooking();
        confirmed.Confirm();
        confirmed.Cancel();
        Assert.Equal(BookingStatus.Cancelled, confirmed.Status);
        Assert.Throws<InvalidOperationException>(confirmed.Complete);
    }

    [Fact]
    public void No_show_requires_confirmation_and_is_terminal()
    {
        var requested = CreateBooking();
        Assert.Throws<InvalidOperationException>(requested.MarkNoShow);

        var confirmed = CreateBooking();
        confirmed.Confirm();
        confirmed.MarkNoShow();

        Assert.Equal(BookingStatus.NoShow, confirmed.Status);
        Assert.Throws<InvalidOperationException>(confirmed.Cancel);
        Assert.Throws<InvalidOperationException>(confirmed.Complete);
    }

    private static Booking CreateBooking()
    {
        var start = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero);
        return Booking.Create(
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            start.AddMinutes(30),
            "Check-up",
            start.AddDays(-1));
    }
}
