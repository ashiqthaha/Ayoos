using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId) : IRequest<BookingModel>;

public sealed class CancelBookingCommandValidator
    : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator() =>
        BookingValidation.AddBookingIdRule(this, command => command.BookingId);
}

internal sealed class CancelBookingCommandHandler(IBookingRepository repository)
    : IRequestHandler<CancelBookingCommand, BookingModel>
{
    public Task<BookingModel> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken) =>
        BookingStatusTransition.ApplyAsync(
            request.BookingId,
            repository,
            booking => booking.Cancel(),
            cancellationToken);
}
