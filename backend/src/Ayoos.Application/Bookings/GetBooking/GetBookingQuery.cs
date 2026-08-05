using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Bookings.GetBooking;

public sealed record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingModel?>;

internal sealed class GetBookingByIdQueryHandler(IBookingRepository repository)
    : IRequestHandler<GetBookingByIdQuery, BookingModel?>
{
    public async Task<BookingModel?> Handle(
        GetBookingByIdQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(
            request.BookingId,
            cancellationToken);
        return booking?.ToModel();
    }
}
