using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.CreateAvailabilityException;

public sealed record CreateAvailabilityExceptionCommand(
    Guid ProviderId,
    DateOnly Date,
    AvailabilityExceptionType ExceptionType,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Reason) : IRequest<AvailabilityExceptionModel>;

public sealed class CreateAvailabilityExceptionCommandValidator
    : AbstractValidator<CreateAvailabilityExceptionCommand>
{
    public CreateAvailabilityExceptionCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.Date).NotEmpty();
        RuleFor(command => command.ExceptionType).IsInEnum();
        RuleFor(command => command.Reason).MaximumLength(500);
        RuleFor(command => command)
            .Must(command => command.ExceptionType switch
            {
                AvailabilityExceptionType.Unavailable =>
                    command.StartTime is null && command.EndTime is null,
                AvailabilityExceptionType.CustomHours =>
                    command.StartTime is not null &&
                    command.EndTime is not null &&
                    command.EndTime > command.StartTime,
                _ => false
            })
            .WithName("EndTime")
            .WithMessage(
                "Unavailable dates cannot define hours; CustomHours requires EndTime after StartTime.");
    }
}

internal sealed class CreateAvailabilityExceptionCommandHandler(
    IProviderRepository providerRepository,
    TimeProvider timeProvider)
    : IRequestHandler<CreateAvailabilityExceptionCommand, AvailabilityExceptionModel>
{
    public async Task<AvailabilityExceptionModel> Handle(
        CreateAvailabilityExceptionCommand request,
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
            request.ExceptionType,
            request.StartTime,
            request.EndTime,
            request.Reason,
            timeProvider.GetUtcNow());
        await providerRepository.AddAvailabilityExceptionAsync(exception, cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return exception.ToModel();
    }
}
