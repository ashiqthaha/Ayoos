using Ayoos.Application.Providers.AddAvailabilityException;
using Ayoos.Application.Providers.CreateAvailability;
using Ayoos.Application.Providers.DeactivateAvailability;
using Ayoos.Application.Providers.UpdateAvailability;

namespace Ayoos.UnitTests;

public sealed class AvailabilityCommandValidatorTests
{
    [Fact]
    public async Task Create_rejects_invalid_hours_and_duration()
    {
        var validator = new CreateAvailabilityCommandValidator();
        var result = await validator.ValidateAsync(new CreateAvailabilityCommand(
            Guid.NewGuid(),
            DayOfWeek.Monday,
            new TimeOnly(17, 0),
            new TimeOnly(9, 0),
            0));

        Assert.Contains(result.Errors, error => error.PropertyName == "EndTime");
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "SlotDurationMinutes");
    }

    [Fact]
    public async Task Update_requires_schedule_id_and_valid_hours()
    {
        var validator = new UpdateAvailabilityCommandValidator();
        var result = await validator.ValidateAsync(new UpdateAvailabilityCommand(
            Guid.NewGuid(),
            Guid.Empty,
            DayOfWeek.Tuesday,
            new TimeOnly(12, 0),
            new TimeOnly(12, 0),
            30));

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "AvailabilityId");
        Assert.Contains(result.Errors, error => error.PropertyName == "EndTime");
    }

    [Fact]
    public async Task Deactivate_requires_provider_and_schedule_ids()
    {
        var validator = new DeactivateAvailabilityCommandValidator();
        var result = await validator.ValidateAsync(
            new DeactivateAvailabilityCommand(Guid.Empty, Guid.Empty));

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Add_exception_rejects_blocked_date_with_custom_hours()
    {
        var validator = new AddAvailabilityExceptionCommandValidator();
        var result = await validator.ValidateAsync(new AddAvailabilityExceptionCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 3),
            true,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            null));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage.Contains("Unavailable dates"));
    }

    [Fact]
    public async Task Add_exception_accepts_custom_hours()
    {
        var validator = new AddAvailabilityExceptionCommandValidator();
        var result = await validator.ValidateAsync(new AddAvailabilityExceptionCommand(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 3),
            false,
            new TimeOnly(10, 0),
            new TimeOnly(14, 0),
            "Extended clinic"));

        Assert.True(result.IsValid);
    }
}
