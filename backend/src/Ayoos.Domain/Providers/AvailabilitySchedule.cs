using Ayoos.Domain.Common;

namespace Ayoos.Domain.Providers;

public sealed class AvailabilitySchedule : Entity
{
    private AvailabilitySchedule()
        : base(Guid.NewGuid())
    {
    }

    private AvailabilitySchedule(
        Guid id,
        Guid providerId,
        string tenantId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
        : base(id)
    {
        ProviderId = providerId;
        TenantId = tenantId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        IsActive = true;
    }

    public Guid ProviderId { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public int SlotDurationMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public static AvailabilitySchedule Create(
        Guid providerId,
        string tenantId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(providerId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ValidateHours(startTime, endTime, slotDurationMinutes);

        return new AvailabilitySchedule(
            Guid.NewGuid(),
            providerId,
            tenantId.Trim(),
            dayOfWeek,
            startTime,
            endTime,
            slotDurationMinutes);
    }

    public void Update(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        ValidateHours(startTime, endTime, slotDurationMinutes);

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
    }

    public void Deactivate() => IsActive = false;

    public bool Overlaps(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime) =>
        IsActive &&
        DayOfWeek == dayOfWeek &&
        StartTime < endTime &&
        EndTime > startTime;

    private static void ValidateHours(
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("StartTime must be before EndTime.", nameof(startTime));
        }

        if (slotDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotDurationMinutes),
                "SlotDurationMinutes must be greater than zero.");
        }
    }
}
