using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.CancelBookingByPatient;

public sealed record CancelBookingByPatientCommand(
    Guid BookingId,
    string? CancellationReason = null) : IRequest<BookingModel>;

public sealed class CancelBookingByPatientCommandValidator
    : AbstractValidator<CancelBookingByPatientCommand>
{
    public CancelBookingByPatientCommandValidator()
    {
        BookingValidation.AddBookingIdRule(this, command => command.BookingId);
        RuleFor(command => command.CancellationReason)
            .MaximumLength(BookingValidation.MaximumCancellationReasonLength);
    }
}

internal sealed class CancelBookingByPatientCommandHandler(
    IBookingRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<CancelBookingByPatientCommand, BookingModel>
{
    public Task<BookingModel> Handle(
        CancelBookingByPatientCommand request,
        CancellationToken cancellationToken) =>
        BookingStatusTransition.ApplyAsync(
            request.BookingId,
            repository,
            booking => booking.CancelByPatient(
                request.CancellationReason,
                timeProvider.GetUtcNow()),
            cancellationToken);
}
