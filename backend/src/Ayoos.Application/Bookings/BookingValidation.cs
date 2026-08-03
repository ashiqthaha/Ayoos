using FluentValidation;
using System.Linq.Expressions;

namespace Ayoos.Application.Bookings;

internal static class BookingValidation
{
    public const int MaximumReasonLength = 1000;

    public static void AddBookingIdRule<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> bookingIdExpression)
    {
        validator.RuleFor(bookingIdExpression)
            .NotEmpty()
            .WithName("BookingId");
    }
}
