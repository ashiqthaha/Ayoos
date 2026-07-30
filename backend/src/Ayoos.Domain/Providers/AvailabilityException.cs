using Ayoos.Domain.Common;

namespace Ayoos.Domain.Providers;

public sealed class AvailabilityException : Entity
{
    private AvailabilityException()
        : base(Guid.NewGuid())
    {
    }

    private AvailabilityException(
        Guid id,
        Guid providerId,
        DateOnly date,
        bool isUnavailable,
        TimeOnly? overrideStartTime,
        TimeOnly? overrideEndTime,
        string? reason)
        : base(id)
    {
        ProviderId = providerId;
        Date = date;
        IsUnavailable = isUnavailable;
        OverrideStartTime = overrideStartTime;
        OverrideEndTime = overrideEndTime;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid ProviderId { get; private set; }

    public DateOnly Date { get; private set; }

    public bool IsUnavailable { get; private set; }

    public TimeOnly? OverrideStartTime { get; private set; }

    public TimeOnly? OverrideEndTime { get; private set; }

    public string? Reason { get; private set; }

    public static AvailabilityException Create(
        Guid providerId,
        DateOnly date,
        bool isUnavailable,
        TimeOnly? overrideStartTime,
        TimeOnly? overrideEndTime,
        string? reason)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(providerId, Guid.Empty);

        if (isUnavailable && (overrideStartTime is not null || overrideEndTime is not null))
        {
            throw new ArgumentException(
                "Unavailable dates cannot also define override hours.");
        }

        if (!isUnavailable &&
            (overrideStartTime is null ||
             overrideEndTime is null ||
             overrideEndTime <= overrideStartTime))
        {
            throw new ArgumentException(
                "Available exceptions require override hours with EndTime after StartTime.");
        }

        return new AvailabilityException(
            Guid.NewGuid(),
            providerId,
            date,
            isUnavailable,
            overrideStartTime,
            overrideEndTime,
            reason);
    }
}
