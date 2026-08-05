using Ayoos.Domain.Bookings;
using Ayoos.Domain.Common;

namespace Ayoos.UnitTests;

public sealed class BookingStatusTransitionTests
{
    [Fact]
    public void Pending_can_be_confirmed_then_completed()
    {
        var booking = CreateBooking();

        Assert.Equal(BookingStatus.Pending, booking.Status);
        booking.Confirm(ChangedAt);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        booking.Complete(ChangedAt.AddMinutes(1));
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(ChangedAt.AddMinutes(1), booking.UpdatedAt);
        Assert.Throws<DomainException>(() => booking.CancelByPatient());
        Assert.Throws<DomainException>(() => booking.MarkNoShow());
        Assert.Throws<DomainException>(() => booking.Confirm());
    }

    [Fact]
    public void Patient_and_provider_cancellation_are_distinct_terminal_states()
    {
        var patientCancellation = CreateBooking();
        patientCancellation.CancelByPatient("Travel conflict", ChangedAt);
        Assert.Equal(BookingStatus.CancelledByPatient, patientCancellation.Status);
        Assert.Equal("Travel conflict", patientCancellation.CancellationReason);
        Assert.Throws<DomainException>(() => patientCancellation.Confirm());

        var providerCancellation = CreateBooking();
        providerCancellation.Confirm(ChangedAt);
        providerCancellation.CancelByProvider("Provider unavailable", ChangedAt.AddMinutes(1));
        Assert.Equal(BookingStatus.CancelledByProvider, providerCancellation.Status);
        Assert.Equal("Provider unavailable", providerCancellation.CancellationReason);
        Assert.Throws<DomainException>(() => providerCancellation.Complete());
    }

    [Fact]
    public void No_show_requires_confirmation_and_is_terminal()
    {
        var pending = CreateBooking();
        Assert.Throws<DomainException>(() => pending.MarkNoShow());

        var confirmed = CreateBooking();
        confirmed.Confirm(ChangedAt);
        confirmed.MarkNoShow(ChangedAt.AddMinutes(1));

        Assert.Equal(BookingStatus.NoShow, confirmed.Status);
        Assert.Throws<DomainException>(() => confirmed.CancelByProvider());
        Assert.Throws<DomainException>(() => confirmed.Complete());
    }

    private static readonly DateTimeOffset ChangedAt =
        new(2030, 1, 6, 10, 0, 0, TimeSpan.Zero);

    private static Booking CreateBooking()
    {
        var start = new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.Zero);
        return Booking.Create(
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            start.AddMinutes(30),
            "Check-up",
            ChangedAt.AddDays(-1));
    }
}
