using FluentValidation;

namespace Ayoos.Application.Practices.CreatePractice;

public sealed class CreatePracticeCommandValidator : AbstractValidator<CreatePracticeCommand>
{
    public CreatePracticeCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .Length(2, 120);

        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(120)
            .Matches(PracticeValidation.SlugPattern)
            .WithMessage("Slug must be lowercase kebab-case.");

        RuleFor(command => command.TimeZone)
            .MaximumLength(100)
            .Must(PracticeValidation.IsValidTimeZone)
            .WithMessage("TimeZone must be a valid IANA time zone identifier.");

        RuleFor(command => command.ContactEmail)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();

        RuleFor(command => command.ContactPhone)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.Address)
            .NotNull()
            .SetValidator(new PracticeAddressModelValidator());

        RuleFor(command => command.RawToken)
            .NotEmpty()
            .MaximumLength(200);
    }
}
