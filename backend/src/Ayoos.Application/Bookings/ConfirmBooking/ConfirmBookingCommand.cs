using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.ConfirmBooking;

public sealed record ConfirmBookingCommand(Guid BookingId) : IRequest<BookingModel>;

public sealed class ConfirmBookingCommandValidator
    : AbstractValidator<ConfirmBookingCommand>
{
    public ConfirmBookingCommandValidator() =>
        BookingValidation.AddBookingIdRule(this, command => command.BookingId);
}

internal sealed class ConfirmBookingCommandHandler(IBookingRepository repository)
    : IRequestHandler<ConfirmBookingCommand, BookingModel>
{
    public Task<BookingModel> Handle(
        ConfirmBookingCommand request,
        CancellationToken cancellationToken) =>
        BookingStatusTransition.ApplyAsync(
            request.BookingId,
            repository,
            booking => booking.Confirm(),
            cancellationToken);
}
