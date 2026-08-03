using Ayoos.Application.Bookings.CancelBooking;
using Ayoos.Application.Bookings.CompleteBooking;
using Ayoos.Application.Bookings.ConfirmBooking;
using Ayoos.Application.Bookings.CreateBooking;
using Ayoos.Application.Bookings.MarkNoShow;

namespace Ayoos.UnitTests;

public sealed class BookingCommandValidatorTests
{
    [Fact]
    public void Create_requires_identifiers_and_an_ordered_time_range()
    {
        var start = new DateTimeOffset(2026, 8, 3, 9, 30, 0, TimeSpan.Zero);
        var result = new CreateBookingCommandValidator().Validate(
            new CreateBookingCommand(
                Guid.Empty,
                Guid.Empty,
                Guid.Empty,
                start,
                start,
                new string('x', 1001)));

        Assert.Contains(result.Errors, error => error.PropertyName == "PatientId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ProviderId");
        Assert.Contains(result.Errors, error => error.PropertyName == "AvailabilityScheduleId");
        Assert.Contains(result.Errors, error => error.PropertyName == "EndTime");
        Assert.Contains(result.Errors, error => error.PropertyName == "Reason");
    }

    [Fact]
    public void Create_accepts_a_valid_slot_request()
    {
        var start = new DateTimeOffset(2026, 8, 3, 9, 30, 0, TimeSpan.Zero);
        var result = new CreateBookingCommandValidator().Validate(
            new CreateBookingCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                start,
                start.AddMinutes(30),
                "Annual check-up"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Every_status_command_requires_a_booking_id()
    {
        Assert.False(new ConfirmBookingCommandValidator()
            .Validate(new ConfirmBookingCommand(Guid.Empty)).IsValid);
        Assert.False(new CancelBookingCommandValidator()
            .Validate(new CancelBookingCommand(Guid.Empty)).IsValid);
        Assert.False(new CompleteBookingCommandValidator()
            .Validate(new CompleteBookingCommand(Guid.Empty)).IsValid);
        Assert.False(new MarkNoShowCommandValidator()
            .Validate(new MarkNoShowCommand(Guid.Empty)).IsValid);
    }
}
