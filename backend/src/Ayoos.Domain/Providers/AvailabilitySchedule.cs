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
        int slotDurationMinutes,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ProviderId = providerId;
        TenantId = tenantId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        IsActive = true;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid ProviderId { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public int SlotDurationMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static AvailabilitySchedule Create(
        Guid providerId,
        string tenantId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateTimeOffset? createdAtUtc = null)
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
            slotDurationMinutes,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void Update(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateTimeOffset? updatedAtUtc = null)
    {
        ValidateHours(startTime, endTime, slotDurationMinutes);

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        UpdatedAtUtc = (updatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }

    public void Deactivate(DateTimeOffset? updatedAtUtc = null)
    {
        IsActive = false;
        UpdatedAtUtc = (updatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }

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
            throw new DomainException("StartTime must be before EndTime.");
        }

        if (slotDurationMinutes <= 0)
        {
            throw new DomainException("SlotDurationMinutes must be greater than zero.");
        }

        var window = endTime - startTime;
        if (window.Ticks % TimeSpan.FromMinutes(slotDurationMinutes).Ticks != 0)
        {
            throw new DomainException(
                "SlotDurationMinutes must divide the availability window evenly.");
        }
    }
}
