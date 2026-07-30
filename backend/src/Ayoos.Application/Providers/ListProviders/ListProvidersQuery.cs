using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Providers.ListProviders;

public sealed record ListProvidersQuery : IRequest<IReadOnlyList<ProviderModel>>;

internal sealed class ListProvidersQueryHandler(IProviderRepository providerRepository)
    : IRequestHandler<ListProvidersQuery, IReadOnlyList<ProviderModel>>
{
    public async Task<IReadOnlyList<ProviderModel>> Handle(
        ListProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var providers = await providerRepository.ListAsync(cancellationToken);
        return providers.Select(provider => provider.ToModel()).ToArray();
    }
}
