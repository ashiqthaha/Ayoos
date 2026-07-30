using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.UpdateProvider;

public sealed record UpdateProviderCommand(
    Guid ProviderId,
    string FirstName,
    string LastName,
    string Credentials,
    string Specialty,
    string Email,
    string Phone) : IRequest<ProviderModel>;

public sealed class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        ProviderValidation.AddProviderRules(
            this,
            command => command.FirstName,
            command => command.LastName,
            command => command.Credentials,
            command => command.Specialty,
            command => command.Email,
            command => command.Phone);
    }
}

internal sealed class UpdateProviderCommandHandler(IProviderRepository providerRepository)
    : IRequestHandler<UpdateProviderCommand, ProviderModel>
{
    public async Task<ProviderModel> Handle(
        UpdateProviderCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken);

        if (provider is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        provider.Update(
            request.FirstName,
            request.LastName,
            request.Credentials,
            request.Specialty,
            request.Email,
            request.Phone);

        await providerRepository.SaveChangesAsync(cancellationToken);
        return provider.ToModel();
    }
}
