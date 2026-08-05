using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.DeleteAvailabilitySchedule;

public sealed record DeleteAvailabilityScheduleCommand(
    Guid ProviderId,
    Guid ScheduleId) : IRequest;

public sealed class DeleteAvailabilityScheduleCommandValidator
    : AbstractValidator<DeleteAvailabilityScheduleCommand>
{
    public DeleteAvailabilityScheduleCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.ScheduleId).NotEmpty();
    }
}

internal sealed class DeleteAvailabilityScheduleCommandHandler(
    IProviderRepository providerRepository,
    TimeProvider timeProvider)
    : IRequestHandler<DeleteAvailabilityScheduleCommand>
{
    public async Task Handle(
        DeleteAvailabilityScheduleCommand request,
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

        schedule.Deactivate(timeProvider.GetUtcNow());
        await providerRepository.SaveChangesAsync(cancellationToken);
    }
}
