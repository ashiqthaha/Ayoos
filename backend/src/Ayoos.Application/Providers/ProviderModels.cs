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

public sealed record AvailabilityRuleInput(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record AvailabilityRuleModel(
    Guid Id,
    Guid ProviderId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

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
    int DurationMinutes);

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

    public static AvailabilityRuleModel ToModel(this AvailabilityRule rule) =>
        new(
            rule.Id,
            rule.ProviderId,
            rule.DayOfWeek,
            rule.StartTime,
            rule.EndTime,
            rule.SlotDurationMinutes,
            rule.EffectiveFrom,
            rule.EffectiveTo);

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
