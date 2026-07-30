using Ayoos.Application.Providers;
using Ayoos.Domain.Providers;

namespace Ayoos.UnitTests;

public sealed class AvailabilitySlotGeneratorTests
{
    private static readonly Guid ProviderId = Guid.Parse(
        "7195c7a5-9f7b-45d9-b201-f1f9bb245156");
    private static readonly DateOnly Monday = new(2026, 8, 3);

    private readonly AvailabilitySlotGenerator _generator = new();

    [Fact]
    public void Weekly_rule_is_split_into_slots()
    {
        var rule = AvailabilityRule.Create(
            ProviderId,
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            30,
            Monday,
            null);

        var slots = _generator.Generate([rule], [], Monday, Monday);

        Assert.Collection(
            slots,
            slot => AssertSlot(slot, 9, 0, 9, 30),
            slot => AssertSlot(slot, 9, 30, 10, 0),
            slot => AssertSlot(slot, 10, 0, 10, 30),
            slot => AssertSlot(slot, 10, 30, 11, 0));
    }

    [Fact]
    public void Unavailable_exception_removes_all_slots_for_the_day()
    {
        var rule = AvailabilityRule.Create(
            ProviderId,
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            30,
            Monday,
            null);
        var exception = AvailabilityException.Create(
            ProviderId,
            Monday,
            true,
            null,
            null,
            "Vacation");

        var slots = _generator.Generate([rule], [exception], Monday, Monday);

        Assert.Empty(slots);
    }

    private static void AssertSlot(
        AvailabilitySlotModel slot,
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        Assert.Equal(Monday, slot.Date);
        Assert.Equal(new TimeOnly(startHour, startMinute), slot.StartTime);
        Assert.Equal(new TimeOnly(endHour, endMinute), slot.EndTime);
        Assert.Equal(30, slot.DurationMinutes);
    }
}
