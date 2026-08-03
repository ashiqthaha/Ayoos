using FluentValidation;
using System.Linq.Expressions;

namespace Ayoos.Application.Providers;

internal static class ProviderValidation
{
    public static void AddProviderRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> firstName,
        Expression<Func<T, string>> lastName,
        Expression<Func<T, string>> credentials,
        Expression<Func<T, string>> specialty,
        Expression<Func<T, string>> email,
        Expression<Func<T, string>> phone)
    {
        validator.RuleFor(firstName)
            .NotEmpty()
            .MaximumLength(100)
            .WithName("FirstName");
        validator.RuleFor(lastName)
            .NotEmpty()
            .MaximumLength(100)
            .WithName("LastName");
        validator.RuleFor(credentials)
            .NotEmpty()
            .MaximumLength(50)
            .WithName("Credentials");
        validator.RuleFor(specialty)
            .NotEmpty()
            .MaximumLength(150)
            .WithName("Specialty");
        validator.RuleFor(email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress()
            .WithName("Email");
        validator.RuleFor(phone)
            .NotEmpty()
            .MaximumLength(50)
            .WithName("Phone");
    }
}

internal static class AvailabilityValidation
{
    public static void AddScheduleRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> providerId,
        Expression<Func<T, DayOfWeek>> dayOfWeek,
        Expression<Func<T, TimeOnly>> startTime,
        Expression<Func<T, TimeOnly>> endTime,
        Expression<Func<T, int>> slotDurationMinutes)
    {
        validator.RuleFor(providerId).NotEmpty().WithName("ProviderId");
        validator.RuleFor(dayOfWeek).IsInEnum().WithName("DayOfWeek");
        validator.RuleFor(endTime)
            .GreaterThan(startTime)
            .WithMessage("EndTime must be after StartTime.");
        validator.RuleFor(slotDurationMinutes)
            .InclusiveBetween(1, 1440)
            .WithName("SlotDurationMinutes");
    }
}
