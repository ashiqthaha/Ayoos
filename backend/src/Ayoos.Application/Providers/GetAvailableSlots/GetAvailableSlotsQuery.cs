using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.GetAvailableSlots;

public sealed record GetAvailableSlotsQuery(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<IReadOnlyList<AvailabilitySlotModel>>;

public sealed class GetAvailableSlotsQueryValidator
    : AbstractValidator<GetAvailableSlotsQuery>
{
    public GetAvailableSlotsQueryValidator()
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

internal sealed class GetAvailableSlotsQueryHandler(
    IProviderRepository providerRepository,
    IBookingRepository bookingRepository,
    AvailabilitySlotGenerator slotGenerator)
    : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<AvailabilitySlotModel>>
{
    public async Task<IReadOnlyList<AvailabilitySlotModel>> Handle(
        GetAvailableSlotsQuery request,
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

        if (!provider.IsActive)
        {
            return [];
        }

        var slots = slotGenerator.Generate(
            provider.AvailabilitySchedules,
            provider.AvailabilityExceptions,
            request.FromDate,
            request.ToDate);

        var bookings = await bookingRepository.GetProviderScheduleAsync(
            request.ProviderId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        if (bookings.Count == 0)
        {
            return slots;
        }

        return slots
            .Where(slot => !bookings.Any(booking =>
                booking.Status != Ayoos.Domain.Bookings.BookingStatus.Cancelled &&
                booking.StartTime < ToUtc(slot.Date, slot.EndTime) &&
                booking.EndTime > ToUtc(slot.Date, slot.StartTime)))
            .ToArray();
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
        new(date.ToDateTime(time), TimeSpan.Zero);
}
