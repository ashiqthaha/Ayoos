using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.DeactivateAvailability;

public sealed record DeactivateAvailabilityCommand(
    Guid ProviderId,
    Guid AvailabilityId) : IRequest;

public sealed class DeactivateAvailabilityCommandValidator
    : AbstractValidator<DeactivateAvailabilityCommand>
{
    public DeactivateAvailabilityCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.AvailabilityId).NotEmpty();
    }
}

internal sealed class DeactivateAvailabilityCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<DeactivateAvailabilityCommand>
{
    public async Task Handle(
        DeactivateAvailabilityCommand request,
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

        schedule.Deactivate();
        await providerRepository.SaveChangesAsync(cancellationToken);
    }
}
