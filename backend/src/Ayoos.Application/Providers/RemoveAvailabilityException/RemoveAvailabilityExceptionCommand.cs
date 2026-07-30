using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.RemoveAvailabilityException;

public sealed record RemoveAvailabilityExceptionCommand(
    Guid ProviderId,
    Guid ExceptionId) : IRequest;

public sealed class RemoveAvailabilityExceptionCommandValidator
    : AbstractValidator<RemoveAvailabilityExceptionCommand>
{
    public RemoveAvailabilityExceptionCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.ExceptionId).NotEmpty();
    }
}

internal sealed class RemoveAvailabilityExceptionCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<RemoveAvailabilityExceptionCommand>
{
    public async Task Handle(
        RemoveAvailabilityExceptionCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            includeAvailability: true,
            cancellationToken);

        if (provider is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        var exception = provider.AvailabilityExceptions.SingleOrDefault(
            item => item.Id == request.ExceptionId);

        if (exception is null)
        {
            throw new NotFoundException(
                $"Availability exception '{request.ExceptionId}' was not found.");
        }

        provider.RemoveAvailabilityException(request.ExceptionId);
        providerRepository.RemoveAvailabilityException(exception);
        await providerRepository.SaveChangesAsync(cancellationToken);
    }
}
