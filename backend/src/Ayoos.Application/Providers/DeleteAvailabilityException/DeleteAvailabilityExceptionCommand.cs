using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.DeleteAvailabilityException;

public sealed record DeleteAvailabilityExceptionCommand(
    Guid ProviderId,
    Guid ExceptionId) : IRequest;

public sealed class DeleteAvailabilityExceptionCommandValidator
    : AbstractValidator<DeleteAvailabilityExceptionCommand>
{
    public DeleteAvailabilityExceptionCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.ExceptionId).NotEmpty();
    }
}

internal sealed class DeleteAvailabilityExceptionCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<DeleteAvailabilityExceptionCommand>
{
    public async Task Handle(
        DeleteAvailabilityExceptionCommand request,
        CancellationToken cancellationToken)
    {
        var exception = await providerRepository.GetAvailabilityExceptionAsync(
            request.ProviderId,
            request.ExceptionId,
            cancellationToken);
        if (exception is null)
        {
            throw new NotFoundException(
                $"Availability exception '{request.ExceptionId}' was not found.");
        }

        providerRepository.RemoveAvailabilityException(exception);
        await providerRepository.SaveChangesAsync(cancellationToken);
    }
}
