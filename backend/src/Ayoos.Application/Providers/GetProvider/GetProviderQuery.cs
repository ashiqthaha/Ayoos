using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Providers.GetProvider;

public sealed record GetProviderQuery(Guid ProviderId) : IRequest<ProviderModel?>;

internal sealed class GetProviderQueryHandler(IProviderRepository providerRepository)
    : IRequestHandler<GetProviderQuery, ProviderModel?>
{
    public async Task<ProviderModel?> Handle(
        GetProviderQuery request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken);

        return provider?.ToModel();
    }
}
