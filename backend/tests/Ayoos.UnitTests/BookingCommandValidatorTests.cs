using Ayoos.Application.Bookings.CancelBookingByPatient;
using Ayoos.Application.Bookings.CancelBookingByProvider;
using Ayoos.Application.Bookings.CompleteBooking;
using Ayoos.Application.Bookings.ConfirmBooking;
using Ayoos.Application.Bookings.CreateBooking;
using Ayoos.Application.Bookings.MarkNoShow;

namespace Ayoos.UnitTests;

public sealed class BookingCommandValidatorTests
{
    [Fact]
    public async Task Create_requires_identifiers_utc_future_time_and_ordered_range()
    {
        var start = DateTimeOffset.UtcNow.AddMinutes(-30);
        var result = await new CreateBookingCommandValidator().ValidateAsync(
            new CreateBookingCommand(
                Guid.Empty,
                Guid.Empty,
                Guid.Empty,
                start.ToOffset(TimeSpan.FromHours(-4)),
                start,
                new string('x', 1001)));

        Assert.Contains(result.Errors, error => error.PropertyName == "PatientId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ProviderId");
        Assert.Contains(result.Errors, error => error.PropertyName == "AvailabilityScheduleId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ScheduledStart");
        Assert.Contains(result.Errors, error => error.PropertyName == "ScheduledEnd");
        Assert.Contains(result.Errors, error => error.PropertyName == "Reason");
    }

    [Fact]
    public async Task Create_accepts_a_well_formed_future_utc_request()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var result = await new CreateBookingCommandValidator().ValidateAsync(
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
        Assert.False(new CancelBookingByPatientCommandValidator()
            .Validate(new CancelBookingByPatientCommand(Guid.Empty)).IsValid);
        Assert.False(new CancelBookingByProviderCommandValidator()
            .Validate(new CancelBookingByProviderCommand(Guid.Empty)).IsValid);
        Assert.False(new CompleteBookingCommandValidator()
            .Validate(new CompleteBookingCommand(Guid.Empty)).IsValid);
        Assert.False(new MarkNoShowCommandValidator()
            .Validate(new MarkNoShowCommand(Guid.Empty)).IsValid);
    }

    [Fact]
    public void Cancellation_reason_is_bounded()
    {
        var reason = new string('x', 501);
        Assert.False(new CancelBookingByPatientCommandValidator()
            .Validate(new CancelBookingByPatientCommand(Guid.NewGuid(), reason)).IsValid);
        Assert.False(new CancelBookingByProviderCommandValidator()
            .Validate(new CancelBookingByProviderCommand(Guid.NewGuid(), reason)).IsValid);
    }
}
