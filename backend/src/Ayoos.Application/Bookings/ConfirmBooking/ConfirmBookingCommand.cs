using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Common;
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

internal sealed class ConfirmBookingCommandHandler(
    IBookingRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<ConfirmBookingCommand, BookingModel>
{
    public async Task<BookingModel> Handle(
        ConfirmBookingCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(
            request.BookingId,
            cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking '{request.BookingId}' was not found.");
        }

        var conflicts = await repository.FindActiveOverlapsAsync(
            booking.ProviderId,
            booking.ScheduledStart,
            booking.ScheduledEnd,
            booking.Id,
            cancellationToken);
        if (conflicts.Count > 0)
        {
            throw new ConflictException(
                "The provider has another active booking that overlaps this time.");
        }

        try
        {
            booking.Confirm(timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            throw new ConflictException(exception.Message);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return booking.ToModel();
    }
}
