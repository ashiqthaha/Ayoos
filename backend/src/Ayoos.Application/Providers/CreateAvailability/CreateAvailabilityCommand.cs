using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.CreateAvailability;

public sealed record CreateAvailabilityCommand(
    Guid ProviderId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes) : IRequest<AvailabilityScheduleModel>;

public sealed class CreateAvailabilityCommandValidator
    : AbstractValidator<CreateAvailabilityCommand>
{
    public CreateAvailabilityCommandValidator()
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

internal sealed class CreateAvailabilityCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<CreateAvailabilityCommand, AvailabilityScheduleModel>
{
    public async Task<AvailabilityScheduleModel> Handle(
        CreateAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken);
        if (provider is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        if (await providerRepository.HasAvailabilityOverlapAsync(
            request.ProviderId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                "The availability schedule overlaps an active schedule for this provider and day.");
        }

        var schedule = AvailabilitySchedule.Create(
            provider.Id,
            provider.PracticeId.ToString("D"),
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes);

        await providerRepository.AddAvailabilityScheduleAsync(
            schedule,
            cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return schedule.ToModel();
    }
}
