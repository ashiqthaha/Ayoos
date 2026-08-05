using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Providers.GetProviderWeeklySchedule;

public sealed record GetProviderWeeklyScheduleQuery(Guid ProviderId)
    : IRequest<ProviderWeeklyScheduleModel>;

internal sealed class GetProviderWeeklyScheduleQueryHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<GetProviderWeeklyScheduleQuery, ProviderWeeklyScheduleModel>
{
    public async Task<ProviderWeeklyScheduleModel> Handle(
        GetProviderWeeklyScheduleQuery request,
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

        var activeSchedules = provider.AvailabilitySchedules
            .Where(schedule => schedule.IsActive)
            .ToArray();
        var days = Enum.GetValues<DayOfWeek>()
            .OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day)
            .Select(day => new ProviderScheduleDayModel(
                day,
                activeSchedules
                    .Where(schedule => schedule.DayOfWeek == day)
                    .OrderBy(schedule => schedule.StartTime)
                    .Select(schedule => schedule.ToModel())
                    .ToArray()))
            .ToArray();

        return new ProviderWeeklyScheduleModel(provider.Id, days);
    }
}
