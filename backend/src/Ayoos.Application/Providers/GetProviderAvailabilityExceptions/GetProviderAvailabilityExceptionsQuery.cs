using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Providers.GetProviderAvailabilityExceptions;

public sealed record GetProviderAvailabilityExceptionsQuery(Guid ProviderId)
    : IRequest<IReadOnlyList<AvailabilityExceptionModel>>;

internal sealed class GetProviderAvailabilityExceptionsQueryHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<
        GetProviderAvailabilityExceptionsQuery,
        IReadOnlyList<AvailabilityExceptionModel>>
{
    public async Task<IReadOnlyList<AvailabilityExceptionModel>> Handle(
        GetProviderAvailabilityExceptionsQuery request,
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

        return provider.AvailabilityExceptions
            .OrderBy(exception => exception.Date)
            .Select(exception => exception.ToModel())
            .ToArray();
    }
}
