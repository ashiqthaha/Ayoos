using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Providers.GetProviderAvailability;

public sealed record GetProviderAvailabilityQuery(Guid ProviderId)
    : IRequest<ProviderAvailabilityModel>;

internal sealed class GetProviderAvailabilityQueryHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<GetProviderAvailabilityQuery, ProviderAvailabilityModel>
{
    public async Task<ProviderAvailabilityModel> Handle(
        GetProviderAvailabilityQuery request,
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

        var schedules = provider.AvailabilitySchedules
            .Where(schedule => schedule.IsActive)
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule => schedule.ToModel())
            .ToArray();
        var exceptions = provider.AvailabilityExceptions
            .OrderBy(exception => exception.Date)
            .Select(exception => exception.ToModel())
            .ToArray();

        return new ProviderAvailabilityModel(provider.Id, schedules, exceptions);
    }
}
