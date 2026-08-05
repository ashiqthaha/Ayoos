using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Bookings.CancelBookingByProvider;

public sealed record CancelBookingByProviderCommand(
    Guid BookingId,
    string? CancellationReason = null) : IRequest<BookingModel>;

public sealed class CancelBookingByProviderCommandValidator
    : AbstractValidator<CancelBookingByProviderCommand>
{
    public CancelBookingByProviderCommandValidator()
    {
        BookingValidation.AddBookingIdRule(this, command => command.BookingId);
        RuleFor(command => command.CancellationReason)
            .MaximumLength(BookingValidation.MaximumCancellationReasonLength);
    }
}

internal sealed class CancelBookingByProviderCommandHandler(
    IBookingRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<CancelBookingByProviderCommand, BookingModel>
{
    public Task<BookingModel> Handle(
        CancelBookingByProviderCommand request,
        CancellationToken cancellationToken) =>
        BookingStatusTransition.ApplyAsync(
            request.BookingId,
            repository,
            booking => booking.CancelByProvider(
                request.CancellationReason,
                timeProvider.GetUtcNow()),
            cancellationToken);
}
