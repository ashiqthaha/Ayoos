using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.PreviewScheduleOverlap;

public sealed record PreviewScheduleOverlapQuery(
    Guid ProviderId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    Guid? ExcludeScheduleId = null) : IRequest<ScheduleOverlapPreviewModel>;

public sealed class PreviewScheduleOverlapQueryValidator
    : AbstractValidator<PreviewScheduleOverlapQuery>
{
    public PreviewScheduleOverlapQueryValidator()
    {
        RuleFor(query => query.ExcludeScheduleId)
            .NotEqual(Guid.Empty)
            .When(query => query.ExcludeScheduleId.HasValue);
        AvailabilityValidation.AddScheduleRules(
            this,
            query => query.ProviderId,
            query => query.DayOfWeek,
            query => query.StartTime,
            query => query.EndTime,
            query => query.SlotDurationMinutes);
    }
}

internal sealed class PreviewScheduleOverlapQueryHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<PreviewScheduleOverlapQuery, ScheduleOverlapPreviewModel>
{
    public async Task<ScheduleOverlapPreviewModel> Handle(
        PreviewScheduleOverlapQuery request,
        CancellationToken cancellationToken)
    {
        if (await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken) is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        return await ScheduleOverlapPreviewer.BuildAsync(
            providerRepository,
            request.ProviderId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.ExcludeScheduleId,
            cancellationToken);
    }
}

internal static class ScheduleOverlapPreviewer
{
    public static async Task<ScheduleOverlapPreviewModel> BuildAsync(
        IProviderRepository providerRepository,
        Guid providerId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludeScheduleId,
        CancellationToken cancellationToken)
    {
        var schedules = await providerRepository.ListActiveAvailabilitySchedulesAsync(
            providerId,
            dayOfWeek,
            cancellationToken);
        var conflicts = AvailabilityOverlapDetector.FindConflicts(
                schedules,
                providerId,
                dayOfWeek,
                startTime,
                endTime,
                excludeScheduleId)
            .Select(schedule => schedule.ToConflictModel())
            .ToArray();

        return new ScheduleOverlapPreviewModel(conflicts.Length > 0, conflicts);
    }
}
