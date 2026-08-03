using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Bookings.GetBooking;

public sealed record GetBookingQuery(Guid BookingId) : IRequest<BookingModel?>;

internal sealed class GetBookingQueryHandler(IBookingRepository repository)
    : IRequestHandler<GetBookingQuery, BookingModel?>
{
    public async Task<BookingModel?> Handle(
        GetBookingQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(
            request.BookingId,
            cancellationToken);
        return booking?.ToModel();
    }
}
