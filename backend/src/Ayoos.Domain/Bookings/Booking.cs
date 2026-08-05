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
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd,
        string? reason,
        DateTimeOffset createdAt)
        : base(id)
    {
        TenantId = tenantId;
        PatientId = patientId;
        ProviderId = providerId;
        AvailabilityScheduleId = availabilityScheduleId;
        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        Status = BookingStatus.Pending;
        Reason = reason;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public string TenantId { get; private set; } = string.Empty;

    public Guid PatientId { get; private set; }

    public Guid ProviderId { get; private set; }

    public Guid? AvailabilityScheduleId { get; private set; }

    public DateTimeOffset ScheduledStart { get; private set; }

    public DateTimeOffset ScheduledEnd { get; private set; }

    public BookingStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public uint RowVersion { get; private set; }

    public bool IsActive => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    public static Booking Create(
        string tenantId,
        Guid patientId,
        Guid providerId,
        Guid? availabilityScheduleId,
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd,
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

        if (scheduledStart >= scheduledEnd)
        {
            throw new DomainException("ScheduledStart must be before ScheduledEnd.");
        }

        return new Booking(
            Guid.NewGuid(),
            tenantId.Trim(),
            patientId,
            providerId,
            availabilityScheduleId,
            scheduledStart.ToUniversalTime(),
            scheduledEnd.ToUniversalTime(),
            Optional(reason),
            createdAt.ToUniversalTime());
    }

    public void Confirm(DateTimeOffset? changedAt = null) => TransitionFrom(
        BookingStatus.Pending,
        BookingStatus.Confirmed,
        changedAt);

    public void CancelByPatient(
        string? cancellationReason = null,
        DateTimeOffset? changedAt = null) => Cancel(
        BookingStatus.CancelledByPatient,
        cancellationReason,
        changedAt);

    public void CancelByProvider(
        string? cancellationReason = null,
        DateTimeOffset? changedAt = null) => Cancel(
        BookingStatus.CancelledByProvider,
        cancellationReason,
        changedAt);

    public void Complete(DateTimeOffset? changedAt = null) => TransitionFrom(
        BookingStatus.Confirmed,
        BookingStatus.Completed,
        changedAt);

    public void MarkNoShow(DateTimeOffset? changedAt = null) => TransitionFrom(
        BookingStatus.Confirmed,
        BookingStatus.NoShow,
        changedAt);

    private void Cancel(
        BookingStatus target,
        string? cancellationReason,
        DateTimeOffset? changedAt)
    {
        if (!IsActive)
        {
            throw InvalidTransition(target);
        }

        Status = target;
        CancellationReason = Optional(cancellationReason);
        Touch(changedAt);
    }

    private void TransitionFrom(
        BookingStatus expected,
        BookingStatus target,
        DateTimeOffset? changedAt)
    {
        if (Status != expected)
        {
            throw InvalidTransition(target);
        }

        Status = target;
        Touch(changedAt);
    }

    private void Touch(DateTimeOffset? changedAt) =>
        UpdatedAt = (changedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();

    private DomainException InvalidTransition(BookingStatus target) =>
        new($"A booking in status '{Status}' cannot transition to '{target}'.");

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
