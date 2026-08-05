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
        string tenantId,
        DateOnly date,
        AvailabilityExceptionType exceptionType,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? reason,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        ProviderId = providerId;
        TenantId = tenantId;
        Date = date;
        ExceptionType = exceptionType;
        StartTime = startTime;
        EndTime = endTime;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid ProviderId { get; private set; }

    public string TenantId { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public AvailabilityExceptionType ExceptionType { get; private set; }

    public TimeOnly? StartTime { get; private set; }

    public TimeOnly? EndTime { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static AvailabilityException Create(
        Guid providerId,
        string tenantId,
        DateOnly date,
        AvailabilityExceptionType exceptionType,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? reason,
        DateTimeOffset? createdAtUtc = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(providerId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!Enum.IsDefined(exceptionType))
        {
            throw new DomainException("ExceptionType is invalid.");
        }

        if (exceptionType == AvailabilityExceptionType.Unavailable &&
            (startTime is not null || endTime is not null))
        {
            throw new DomainException(
                "Unavailable dates cannot also define override hours.");
        }

        if (exceptionType == AvailabilityExceptionType.CustomHours &&
            (startTime is null || endTime is null || endTime <= startTime))
        {
            throw new DomainException(
                "CustomHours exceptions require StartTime and EndTime, with EndTime after StartTime.");
        }

        return new AvailabilityException(
            Guid.NewGuid(),
            providerId,
            tenantId.Trim(),
            date,
            exceptionType,
            startTime,
            endTime,
            reason,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }
}
