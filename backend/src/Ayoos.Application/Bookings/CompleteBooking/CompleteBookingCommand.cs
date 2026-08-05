using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.CompleteBooking;

public sealed record CompleteBookingCommand(Guid BookingId) : IRequest<BookingModel>;

public sealed class CompleteBookingCommandValidator
    : AbstractValidator<CompleteBookingCommand>
{
    public CompleteBookingCommandValidator() =>
        BookingValidation.AddBookingIdRule(this, command => command.BookingId);
}

internal sealed class CompleteBookingCommandHandler(
    IBookingRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteBookingCommand, BookingModel>
{
    public Task<BookingModel> Handle(
        CompleteBookingCommand request,
        CancellationToken cancellationToken) =>
        BookingStatusTransition.ApplyAsync(
            request.BookingId,
            repository,
            booking => booking.Complete(timeProvider.GetUtcNow()),
            cancellationToken);
}
