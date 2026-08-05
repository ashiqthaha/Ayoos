using Ayoos.Application.Providers;
using Ayoos.Domain.Providers;

namespace Ayoos.Application.Bookings;

internal static class BookingSlotMatcher
{
    public static AvailabilitySlotModel? Find(
        Provider provider,
        Guid? availabilityScheduleId,
        DateTimeOffset scheduledStart,
        DateTimeOffset scheduledEnd,
        AvailabilitySlotGenerator generator)
    {
        var startUtc = scheduledStart.ToUniversalTime();
        var endUtc = scheduledEnd.ToUniversalTime();
        var date = DateOnly.FromDateTime(startUtc.UtcDateTime);

        return generator
            .Generate(
                provider.AvailabilitySchedules,
                provider.AvailabilityExceptions,
                date,
                date)
            .SingleOrDefault(slot =>
                ToUtc(slot.Date, slot.StartTime) == startUtc &&
                ToUtc(slot.Date, slot.EndTime) == endUtc &&
                (!availabilityScheduleId.HasValue ||
                    slot.AvailabilityScheduleId == availabilityScheduleId));
    }

    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
        new(date.ToDateTime(time), TimeSpan.Zero);
}
