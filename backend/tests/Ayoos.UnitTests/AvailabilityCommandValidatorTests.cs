using Ayoos.Application.Providers.CreateAvailabilityException;
using Ayoos.Application.Providers.CreateAvailabilitySchedule;
using Ayoos.Application.Providers.DeleteAvailabilityException;
using Ayoos.Application.Providers.DeleteAvailabilitySchedule;
using Ayoos.Application.Providers.UpdateAvailabilitySchedule;
using Ayoos.Domain.Providers;

namespace Ayoos.UnitTests;

public sealed class AvailabilityCommandValidatorTests
{
    [Fact]
    public async Task Create_rejects_invalid_hours_and_non_dividing_duration()
    {
        var validator = new CreateAvailabilityScheduleCommandValidator();
        var invalidHours = await validator.ValidateAsync(
            new CreateAvailabilityScheduleCommand(
                Guid.NewGuid(),
                DayOfWeek.Monday,
                new TimeOnly(17, 0),
                new TimeOnly(9, 0),
                30));
        var nonDividing = await validator.ValidateAsync(
            new CreateAvailabilityScheduleCommand(
                Guid.NewGuid(),
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(10, 0),
                40));

        Assert.Contains(invalidHours.Errors, error => error.PropertyName == "EndTime");
        Assert.Contains(
            nonDividing.Errors,
            error => error.PropertyName == "SlotDurationMinutes");
    }

    [Fact]
    public async Task Update_requires_schedule_id()
    {
        var validator = new UpdateAvailabilityScheduleCommandValidator();
        var result = await validator.ValidateAsync(
            new UpdateAvailabilityScheduleCommand(
                Guid.NewGuid(),
                Guid.Empty,
                DayOfWeek.Tuesday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30));

        Assert.Contains(result.Errors, error => error.PropertyName == "ScheduleId");
    }

    [Fact]
    public async Task Delete_commands_require_both_ids()
    {
        var scheduleResult = await new DeleteAvailabilityScheduleCommandValidator()
            .ValidateAsync(new DeleteAvailabilityScheduleCommand(Guid.Empty, Guid.Empty));
        var exceptionResult = await new DeleteAvailabilityExceptionCommandValidator()
            .ValidateAsync(new DeleteAvailabilityExceptionCommand(Guid.Empty, Guid.Empty));

        Assert.Equal(2, scheduleResult.Errors.Count);
        Assert.Equal(2, exceptionResult.Errors.Count);
    }

    [Fact]
    public async Task Create_exception_rejects_unavailable_date_with_custom_hours()
    {
        var validator = new CreateAvailabilityExceptionCommandValidator();
        var result = await validator.ValidateAsync(
            new CreateAvailabilityExceptionCommand(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 3),
                AvailabilityExceptionType.Unavailable,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                null));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage.Contains("Unavailable dates"));
    }

    [Fact]
    public async Task Create_exception_accepts_custom_hours()
    {
        var validator = new CreateAvailabilityExceptionCommandValidator();
        var result = await validator.ValidateAsync(
            new CreateAvailabilityExceptionCommand(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 3),
                AvailabilityExceptionType.CustomHours,
                new TimeOnly(10, 0),
                new TimeOnly(14, 0),
                "Extended clinic"));

        Assert.True(result.IsValid);
    }
}
