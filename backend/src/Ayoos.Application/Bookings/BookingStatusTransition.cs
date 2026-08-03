using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Bookings;

namespace Ayoos.Application.Bookings;

internal static class BookingStatusTransition
{
    public static async Task<BookingModel> ApplyAsync(
        Guid bookingId,
        IBookingRepository repository,
        Action<Booking> transition,
        CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking '{bookingId}' was not found.");
        }

        try
        {
            transition(booking);
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return booking.ToModel();
    }
}
