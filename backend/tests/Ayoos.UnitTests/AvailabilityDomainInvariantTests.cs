using Ayoos.Domain.Common;
using Ayoos.Domain.Providers;

namespace Ayoos.UnitTests;

public sealed class AvailabilityDomainInvariantTests
{
    private static readonly Guid ProviderId = Guid.NewGuid();
    private const string TenantId = "f993c686-f775-44e5-a673-ea23cb00408b";

    [Fact]
    public void Schedule_rejects_end_time_at_or_before_start_time()
    {
        var exception = Assert.Throws<DomainException>(() =>
            AvailabilitySchedule.Create(
                ProviderId,
                TenantId,
                DayOfWeek.Monday,
                new TimeOnly(12, 0),
                new TimeOnly(12, 0),
                30));

        Assert.Contains("before EndTime", exception.Message);
    }

    [Fact]
    public void Schedule_rejects_non_positive_duration()
    {
        var exception = Assert.Throws<DomainException>(() =>
            AvailabilitySchedule.Create(
                ProviderId,
                TenantId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                0));

        Assert.Contains("greater than zero", exception.Message);
    }

    [Fact]
    public void Schedule_rejects_duration_that_does_not_evenly_divide_window()
    {
        var exception = Assert.Throws<DomainException>(() =>
            AvailabilitySchedule.Create(
                ProviderId,
                TenantId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(10, 0),
                40));

        Assert.Contains("divide", exception.Message);
    }

    [Fact]
    public void Unavailable_exception_rejects_hours()
    {
        Assert.Throws<DomainException>(() => AvailabilityException.Create(
            ProviderId,
            TenantId,
            new DateOnly(2026, 8, 10),
            AvailabilityExceptionType.Unavailable,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            null));
    }

    [Fact]
    public void Custom_hours_exception_requires_a_valid_window()
    {
        Assert.Throws<DomainException>(() => AvailabilityException.Create(
            ProviderId,
            TenantId,
            new DateOnly(2026, 8, 10),
            AvailabilityExceptionType.CustomHours,
            null,
            null,
            null));
    }
}
