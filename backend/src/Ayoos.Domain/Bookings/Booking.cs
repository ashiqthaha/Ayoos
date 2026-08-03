using Ayoos.Domain.Common;

namespace Ayoos.Domain.Bookings;

public sealed class Booking : Entity
{
    private Booking()
        : base(Guid.NewGuid())
    {
    }

    private Booking(
        Guid id,
        string tenantId,
        Guid patientId,
        Guid providerId,
        Guid? availabilityScheduleId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? reason,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        PatientId = patientId;
        ProviderId = providerId;
        AvailabilityScheduleId = availabilityScheduleId;
        StartTime = startTime;
        EndTime = endTime;
        Status = BookingStatus.Requested;
        Reason = reason;
        CreatedAt = createdAt;
    }

    public string TenantId { get; private set; } = string.Empty;

    public Guid PatientId { get; private set; }

    public Guid ProviderId { get; private set; }

    public Guid? AvailabilityScheduleId { get; private set; }

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset EndTime { get; private set; }

    public BookingStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Booking Create(
        string tenantId,
        Guid patientId,
        Guid providerId,
        Guid? availabilityScheduleId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? reason,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfEqual(patientId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(providerId, Guid.Empty);
        if (availabilityScheduleId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(availabilityScheduleId));
        }

        if (startTime >= endTime)
        {
            throw new ArgumentException(
                "StartTime must be before EndTime.",
                nameof(startTime));
        }

        return new Booking(
            Guid.NewGuid(),
            tenantId.Trim(),
            patientId,
            providerId,
            availabilityScheduleId,
            startTime.ToUniversalTime(),
            endTime.ToUniversalTime(),
            Optional(reason),
            createdAt.ToUniversalTime());
    }

    public void Confirm() => TransitionFrom(
        BookingStatus.Requested,
        BookingStatus.Confirmed);

    public void Cancel()
    {
        if (Status is not BookingStatus.Requested and not BookingStatus.Confirmed)
        {
            throw InvalidTransition(BookingStatus.Cancelled);
        }

        Status = BookingStatus.Cancelled;
    }

    public void Complete() => TransitionFrom(
        BookingStatus.Confirmed,
        BookingStatus.Completed);

    public void MarkNoShow() => TransitionFrom(
        BookingStatus.Confirmed,
        BookingStatus.NoShow);

    private void TransitionFrom(BookingStatus expected, BookingStatus target)
    {
        if (Status != expected)
        {
            throw InvalidTransition(target);
        }

        Status = target;
    }

    private InvalidOperationException InvalidTransition(BookingStatus target) =>
        new($"A booking in status '{Status}' cannot transition to '{target}'.");

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
