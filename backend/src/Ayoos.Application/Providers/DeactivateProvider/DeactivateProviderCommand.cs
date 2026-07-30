using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.DeactivateProvider;

public sealed record DeactivateProviderCommand(Guid ProviderId) : IRequest<ProviderModel>;

public sealed class DeactivateProviderCommandValidator
    : AbstractValidator<DeactivateProviderCommand>
{
    public DeactivateProviderCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
    }
}

internal sealed class DeactivateProviderCommandHandler(IProviderRepository providerRepository)
    : IRequestHandler<DeactivateProviderCommand, ProviderModel>
{
    public async Task<ProviderModel> Handle(
        DeactivateProviderCommand request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken);

        if (provider is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        provider.Deactivate();
        await providerRepository.SaveChangesAsync(cancellationToken);

        return provider.ToModel();
    }
}
