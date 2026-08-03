using Ayoos.Domain.Providers;

namespace Ayoos.Application.Providers;

public sealed class AvailabilitySlotGenerator
{
    public IReadOnlyList<AvailabilitySlotModel> Generate(
        IReadOnlyCollection<AvailabilitySchedule> schedules,
        IReadOnlyCollection<AvailabilityException> exceptions,
        DateOnly fromDate,
        DateOnly toDate)
    {
        if (toDate < fromDate)
        {
            throw new ArgumentException("ToDate must be on or after FromDate.", nameof(toDate));
        }

        var slots = new List<AvailabilitySlotModel>();
        var exceptionsByDate = exceptions.ToDictionary(item => item.Date);

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            exceptionsByDate.TryGetValue(date, out var exception);
            if (exception?.IsUnavailable == true)
            {
                continue;
            }

            var dailySchedules = schedules
                .Where(schedule =>
                    schedule.IsActive &&
                    schedule.DayOfWeek == date.DayOfWeek)
                .OrderBy(schedule => schedule.StartTime)
                .ToArray();

            if (exception is not null)
            {
                var duration = dailySchedules.FirstOrDefault()?.SlotDurationMinutes ?? 30;
                AddSlots(
                    slots,
                    date,
                    exception.OverrideStartTime!.Value,
                    exception.OverrideEndTime!.Value,
                    duration,
                    null);
                continue;
            }

            foreach (var schedule in dailySchedules)
            {
                AddSlots(
                    slots,
                    date,
                    schedule.StartTime,
                    schedule.EndTime,
                    schedule.SlotDurationMinutes,
                    schedule.Id);
            }
        }

        return slots;
    }

    private static void AddSlots(
        ICollection<AvailabilitySlotModel> slots,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int durationMinutes,
        Guid? availabilityScheduleId)
    {
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var cursor = startTime.ToTimeSpan();
        var end = endTime.ToTimeSpan();

        while (cursor + duration <= end)
        {
            var slotStart = TimeOnly.FromTimeSpan(cursor);
            var slotEnd = TimeOnly.FromTimeSpan(cursor + duration);
            slots.Add(
                new AvailabilitySlotModel(
                    date,
                    slotStart,
                    slotEnd,
                    durationMinutes,
                    availabilityScheduleId));
            cursor += duration;
        }
    }
}
