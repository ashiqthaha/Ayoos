using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Providers.PreviewScheduleOverlap;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.UpdateAvailabilitySchedule;

public sealed record UpdateAvailabilityScheduleCommand(
    Guid ProviderId,
    Guid ScheduleId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    bool ConfirmOverlap = false) : IRequest<AvailabilityScheduleMutationResult>;

public sealed class UpdateAvailabilityScheduleCommandValidator
    : AbstractValidator<UpdateAvailabilityScheduleCommand>
{
    public UpdateAvailabilityScheduleCommandValidator()
    {
        RuleFor(command => command.ScheduleId).NotEmpty();
        AvailabilityValidation.AddScheduleRules(
            this,
            command => command.ProviderId,
            command => command.DayOfWeek,
            command => command.StartTime,
            command => command.EndTime,
            command => command.SlotDurationMinutes);
    }
}

internal sealed class UpdateAvailabilityScheduleCommandHandler(
    IProviderRepository providerRepository,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateAvailabilityScheduleCommand, AvailabilityScheduleMutationResult>
{
    public async Task<AvailabilityScheduleMutationResult> Handle(
        UpdateAvailabilityScheduleCommand request,
        CancellationToken cancellationToken)
    {
        var schedule = await providerRepository.GetAvailabilityScheduleAsync(
            request.ProviderId,
            request.ScheduleId,
            cancellationToken);
        if (schedule is null)
        {
            throw new NotFoundException(
                $"Availability schedule '{request.ScheduleId}' was not found.");
        }

        var preview = await ScheduleOverlapPreviewer.BuildAsync(
            providerRepository,
            request.ProviderId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.ScheduleId,
            cancellationToken);
        if (preview.HasConflicts && !request.ConfirmOverlap)
        {
            return new AvailabilityScheduleMutationResult(null, preview);
        }

        schedule.Update(
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes,
            timeProvider.GetUtcNow());
        await providerRepository.SaveChangesAsync(cancellationToken);

        return new AvailabilityScheduleMutationResult(schedule.ToModel(), preview);
    }
}
