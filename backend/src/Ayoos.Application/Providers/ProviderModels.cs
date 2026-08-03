using Ayoos.Domain.Providers;

namespace Ayoos.Application.Providers;

public sealed record ProviderModel(
    Guid Id,
    Guid PracticeId,
    string FirstName,
    string LastName,
    string Credentials,
    string Specialty,
    string Email,
    string Phone,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record AvailabilityScheduleModel(
    Guid Id,
    Guid ProviderId,
    string TenantId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    bool IsActive);

public sealed record AvailabilityExceptionModel(
    Guid Id,
    Guid ProviderId,
    DateOnly Date,
    bool IsUnavailable,
    TimeOnly? OverrideStartTime,
    TimeOnly? OverrideEndTime,
    string? Reason);

public sealed record AvailabilitySlotModel(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int DurationMinutes,
    Guid? AvailabilityScheduleId);

public sealed record ProviderAvailabilityModel(
    Guid ProviderId,
    IReadOnlyList<AvailabilityScheduleModel> Schedules,
    IReadOnlyList<AvailabilityExceptionModel> Exceptions);

internal static class ProviderMappings
{
    public static ProviderModel ToModel(this Provider provider) =>
        new(
            provider.Id,
            provider.PracticeId,
            provider.FirstName,
            provider.LastName,
            provider.Credentials,
            provider.Specialty,
            provider.Email,
            provider.Phone,
            provider.IsActive,
            provider.CreatedAtUtc);

    public static AvailabilityScheduleModel ToModel(
        this AvailabilitySchedule schedule) =>
        new(
            schedule.Id,
            schedule.ProviderId,
            schedule.TenantId,
            schedule.DayOfWeek,
            schedule.StartTime,
            schedule.EndTime,
            schedule.SlotDurationMinutes,
            schedule.IsActive);

    public static AvailabilityExceptionModel ToModel(
        this AvailabilityException exception) =>
        new(
            exception.Id,
            exception.ProviderId,
            exception.Date,
            exception.IsUnavailable,
            exception.OverrideStartTime,
            exception.OverrideEndTime,
            exception.Reason);
}
