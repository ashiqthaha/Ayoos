using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.GetProviderSchedule;

public sealed record GetProviderScheduleQuery(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<IReadOnlyList<BookingModel>>;

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
    IBookingRepository bookingRepository)
    : IRequestHandler<GetProviderScheduleQuery, IReadOnlyList<BookingModel>>
{
    public async Task<IReadOnlyList<BookingModel>> Handle(
        GetProviderScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken);
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
        return bookings.Select(booking => booking.ToModel()).ToArray();
    }
}
