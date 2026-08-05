namespace Ayoos.Domain.Providers;

public static class AvailabilityOverlapDetector
{
    public static IReadOnlyList<AvailabilitySchedule> FindConflicts(
        IEnumerable<AvailabilitySchedule> existingSchedules,
        Guid providerId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeScheduleId = null) =>
        existingSchedules
            .Where(schedule =>
                schedule.ProviderId == providerId &&
                schedule.Id != excludeScheduleId &&
                schedule.Overlaps(dayOfWeek, startTime, endTime))
            .OrderBy(schedule => schedule.StartTime)
            .ToArray();
}
