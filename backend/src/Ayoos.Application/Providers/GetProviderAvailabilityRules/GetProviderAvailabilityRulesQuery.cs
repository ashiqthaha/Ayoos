using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Providers.GetProviderAvailabilityRules;

public sealed record GetProviderAvailabilityRulesQuery(Guid ProviderId)
    : IRequest<IReadOnlyList<AvailabilityRuleModel>>;

internal sealed class GetProviderAvailabilityRulesQueryHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<GetProviderAvailabilityRulesQuery, IReadOnlyList<AvailabilityRuleModel>>
{
    public async Task<IReadOnlyList<AvailabilityRuleModel>> Handle(
        GetProviderAvailabilityRulesQuery request,
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

        return provider.AvailabilityRules
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartTime)
            .Select(rule => rule.ToModel())
            .ToArray();
    }
}
