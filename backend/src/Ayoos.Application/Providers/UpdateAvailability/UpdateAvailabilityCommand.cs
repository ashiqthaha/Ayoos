using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.UpdateAvailability;

public sealed record UpdateAvailabilityCommand(
    Guid ProviderId,
    Guid AvailabilityId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes) : IRequest<AvailabilityScheduleModel>;

public sealed class UpdateAvailabilityCommandValidator
    : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityCommandValidator()
    {
        RuleFor(command => command.AvailabilityId).NotEmpty();
        AvailabilityValidation.AddScheduleRules(
            this,
            command => command.ProviderId,
            command => command.DayOfWeek,
            command => command.StartTime,
            command => command.EndTime,
            command => command.SlotDurationMinutes);
    }
}

internal sealed class UpdateAvailabilityCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<UpdateAvailabilityCommand, AvailabilityScheduleModel>
{
    public async Task<AvailabilityScheduleModel> Handle(
        UpdateAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var schedule = await providerRepository.GetAvailabilityScheduleAsync(
            request.ProviderId,
            request.AvailabilityId,
            cancellationToken);
        if (schedule is null)
        {
            throw new NotFoundException(
                $"Availability schedule '{request.AvailabilityId}' was not found.");
        }

        if (await providerRepository.HasAvailabilityOverlapAsync(
            request.ProviderId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.AvailabilityId,
            cancellationToken))
        {
            throw new ConflictException(
                "The availability schedule overlaps an active schedule for this provider and day.");
        }

        schedule.Update(
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return schedule.ToModel();
    }
}
