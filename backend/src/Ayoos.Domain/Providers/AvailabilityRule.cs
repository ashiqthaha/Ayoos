using Ayoos.Domain.Common;

namespace Ayoos.Domain.Providers;

public sealed class AvailabilityRule : Entity
{
    private AvailabilityRule()
        : base(Guid.NewGuid())
    {
    }

    private AvailabilityRule(
        Guid id,
        Guid providerId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
        : base(id)
    {
        ProviderId = providerId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid ProviderId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public int SlotDurationMinutes { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public static AvailabilityRule Create(
        Guid providerId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(providerId, Guid.Empty);

        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));
        }

        if (slotDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotDurationMinutes),
                "SlotDurationMinutes must be greater than zero.");
        }

        if (effectiveTo < effectiveFrom)
        {
            throw new ArgumentException(
                "EffectiveTo must be on or after EffectiveFrom.",
                nameof(effectiveTo));
        }

        return new AvailabilityRule(
            Guid.NewGuid(),
            providerId,
            dayOfWeek,
            startTime,
            endTime,
            slotDurationMinutes,
            effectiveFrom,
            effectiveTo);
    }
}
