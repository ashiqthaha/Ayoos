using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Providers.PreviewScheduleOverlap;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.CreateAvailabilitySchedule;

public sealed record CreateAvailabilityScheduleCommand(
    Guid ProviderId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    bool ConfirmOverlap = false) : IRequest<AvailabilityScheduleMutationResult>;

public sealed class CreateAvailabilityScheduleCommandValidator
    : AbstractValidator<CreateAvailabilityScheduleCommand>
{
    public CreateAvailabilityScheduleCommandValidator()
    {
        AvailabilityValidation.AddScheduleRules(
            this,
            command => command.ProviderId,
            command => command.DayOfWeek,
            command => command.StartTime,
            command => command.EndTime,
            command => command.SlotDurationMinutes);
    }
}

internal sealed class CreateAvailabilityScheduleCommandHandler(
    IProviderRepository providerRepository,
    TimeProvider timeProvider)
    : IRequestHandler<CreateAvailabilityScheduleCommand, AvailabilityScheduleMutationResult>
{
    public async Task<AvailabilityScheduleMutationResult> Handle(
        CreateAvailabilityScheduleCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken);
        if (provider is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        var preview = await ScheduleOverlapPreviewer.BuildAsync(
            providerRepository,
            request.ProviderId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            null,
            cancellationToken);
        if (preview.HasConflicts && !request.ConfirmOverlap)
        {
            return new AvailabilityScheduleMutationResult(null, preview);
        }

        var schedule = AvailabilitySchedule.Create(
            provider.Id,
            provider.PracticeId.ToString("D"),
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes,
            timeProvider.GetUtcNow());
        await providerRepository.AddAvailabilityScheduleAsync(schedule, cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return new AvailabilityScheduleMutationResult(schedule.ToModel(), preview);
    }
}
