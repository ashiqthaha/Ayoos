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

public sealed class AvailabilityRuleInputValidator : AbstractValidator<AvailabilityRuleInput>
{
    public AvailabilityRuleInputValidator()
    {
        RuleFor(rule => rule.DayOfWeek).IsInEnum();
        RuleFor(rule => rule.EndTime)
            .GreaterThan(rule => rule.StartTime)
            .WithMessage("EndTime must be after StartTime.");
        RuleFor(rule => rule.SlotDurationMinutes)
            .InclusiveBetween(1, 1440);
        RuleFor(rule => rule.EffectiveFrom).NotEmpty();
        RuleFor(rule => rule.EffectiveTo)
            .Must((rule, effectiveTo) =>
                effectiveTo is null || effectiveTo >= rule.EffectiveFrom)
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");
    }
}
