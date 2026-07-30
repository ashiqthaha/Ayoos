using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.CreateProvider;

public sealed record CreateProviderCommand(
    string FirstName,
    string LastName,
    string Credentials,
    string Specialty,
    string Email,
    string Phone) : IRequest<ProviderModel>;

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator()
    {
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

internal sealed class CreateProviderCommandHandler(
    IProviderRepository providerRepository,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<CreateProviderCommand, ProviderModel>
{
    public async Task<ProviderModel> Handle(
        CreateProviderCommand request,
        CancellationToken cancellationToken)
    {
        var provider = Provider.Create(
            currentPractice.PracticeId,
            request.FirstName,
            request.LastName,
            request.Credentials,
            request.Specialty,
            request.Email,
            request.Phone,
            DateTimeOffset.UtcNow);

        await providerRepository.AddAsync(provider, cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return provider.ToModel();
    }
}
