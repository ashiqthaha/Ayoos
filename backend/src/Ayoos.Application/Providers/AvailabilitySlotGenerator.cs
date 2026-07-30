using Ayoos.Domain.Providers;

namespace Ayoos.Application.Providers;

public sealed class AvailabilitySlotGenerator
{
    public IReadOnlyList<AvailabilitySlotModel> Generate(
        IReadOnlyCollection<AvailabilityRule> rules,
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

            var dailyRules = rules
                .Where(rule =>
                    rule.DayOfWeek == date.DayOfWeek &&
                    rule.EffectiveFrom <= date &&
                    (rule.EffectiveTo is null || rule.EffectiveTo >= date))
                .OrderBy(rule => rule.StartTime)
                .ToArray();

            if (exception is not null)
            {
                var duration = dailyRules.FirstOrDefault()?.SlotDurationMinutes ?? 30;
                AddSlots(
                    slots,
                    date,
                    exception.OverrideStartTime!.Value,
                    exception.OverrideEndTime!.Value,
                    duration);
                continue;
            }

            foreach (var rule in dailyRules)
            {
                AddSlots(
                    slots,
                    date,
                    rule.StartTime,
                    rule.EndTime,
                    rule.SlotDurationMinutes);
            }
        }

        return slots;
    }

    private static void AddSlots(
        ICollection<AvailabilitySlotModel> slots,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int durationMinutes)
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
                    durationMinutes));
            cursor += duration;
        }
    }
}
