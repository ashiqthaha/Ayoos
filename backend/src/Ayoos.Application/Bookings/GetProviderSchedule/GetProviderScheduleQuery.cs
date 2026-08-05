using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.GetProviderSchedule;

public sealed record GetProviderScheduleQuery(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<ProviderScheduleModel>;

public sealed class GetProviderScheduleQueryValidator
    : AbstractValidator<GetProviderScheduleQuery>
{
    public GetProviderScheduleQueryValidator()
    {
        RuleFor(query => query.ProviderId).NotEmpty();
        RuleFor(query => query.ToDate)
            .GreaterThanOrEqualTo(query => query.FromDate)
            .WithMessage("ToDate must be on or after FromDate.");
        RuleFor(query => query)
            .Must(query => query.ToDate.DayNumber - query.FromDate.DayNumber <= 366)
            .WithName("ToDate")
            .WithMessage("The requested date range cannot exceed 366 days.");
    }
}

internal sealed class GetProviderScheduleQueryHandler(
    IProviderRepository providerRepository,
    IBookingRepository bookingRepository,
    AvailabilitySlotGenerator slotGenerator)
    : IRequestHandler<GetProviderScheduleQuery, ProviderScheduleModel>
{
    public async Task<ProviderScheduleModel> Handle(
        GetProviderScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            includeAvailability: true,
            cancellationToken);
        if (provider is null)
        {
            throw new NotFoundException(
                $"Provider '{request.ProviderId}' was not found.");
        }

        var bookings = await bookingRepository.GetProviderScheduleAsync(
            request.ProviderId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        var generatedSlots = provider.IsActive
            ? slotGenerator.Generate(
                provider.AvailabilitySchedules,
                provider.AvailabilityExceptions,
                request.FromDate,
                request.ToDate)
            : [];
        var openSlots = generatedSlots
            .Where(slot => !bookings.Any(booking =>
                booking.IsActive &&
                booking.ScheduledStart < BookingSlotMatcher.ToUtc(slot.Date, slot.EndTime) &&
                booking.ScheduledEnd > BookingSlotMatcher.ToUtc(slot.Date, slot.StartTime)))
            .ToArray();

        return new ProviderScheduleModel(
            provider.Id,
            request.FromDate,
            request.ToDate,
            bookings.Select(booking => booking.ToModel()).ToArray(),
            openSlots);
    }
}
