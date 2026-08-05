using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.MarkNoShow;

public sealed record MarkNoShowCommand(Guid BookingId) : IRequest<BookingModel>;

public sealed class MarkNoShowCommandValidator
    : AbstractValidator<MarkNoShowCommand>
{
    public MarkNoShowCommandValidator() =>
        BookingValidation.AddBookingIdRule(this, command => command.BookingId);
}

internal sealed class MarkNoShowCommandHandler(
    IBookingRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<MarkNoShowCommand, BookingModel>
{
    public Task<BookingModel> Handle(
        MarkNoShowCommand request,
        CancellationToken cancellationToken) =>
        BookingStatusTransition.ApplyAsync(
            request.BookingId,
            repository,
            booking => booking.MarkNoShow(timeProvider.GetUtcNow()),
            cancellationToken);
}
