using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.AddAvailabilityException;

public sealed record AddAvailabilityExceptionCommand(
    Guid ProviderId,
    DateOnly Date,
    bool IsUnavailable,
    TimeOnly? OverrideStartTime,
    TimeOnly? OverrideEndTime,
    string? Reason) : IRequest<AvailabilityExceptionModel>;

public sealed class AddAvailabilityExceptionCommandValidator
    : AbstractValidator<AddAvailabilityExceptionCommand>
{
    public AddAvailabilityExceptionCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.Date).NotEmpty();
        RuleFor(command => command.Reason).MaximumLength(500);
        RuleFor(command => command)
            .Must(command =>
                command.IsUnavailable
                    ? command.OverrideStartTime is null && command.OverrideEndTime is null
                    : command.OverrideStartTime is not null &&
                      command.OverrideEndTime is not null &&
                      command.OverrideEndTime > command.OverrideStartTime)
            .WithName("OverrideEndTime")
            .WithMessage(
                "Unavailable dates cannot define hours; custom hours require EndTime after StartTime.");
    }
}

internal sealed class AddAvailabilityExceptionCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<AddAvailabilityExceptionCommand, AvailabilityExceptionModel>
{
    public async Task<AvailabilityExceptionModel> Handle(
        AddAvailabilityExceptionCommand request,
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

        if (provider.AvailabilityExceptions.Any(item => item.Date == request.Date))
        {
            throw new ConflictException(
                $"An availability exception already exists for {request.Date:yyyy-MM-dd}.");
        }

        var exception = AvailabilityException.Create(
            provider.Id,
            provider.PracticeId.ToString("D"),
            request.Date,
            request.IsUnavailable,
            request.OverrideStartTime,
            request.OverrideEndTime,
            request.Reason);

        provider.AddAvailabilityException(exception);
        await providerRepository.AddAvailabilityExceptionAsync(
            exception,
            cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return exception.ToModel();
    }
}
