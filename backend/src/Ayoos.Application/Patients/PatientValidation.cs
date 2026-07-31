using Ayoos.Domain.Patients;
using FluentValidation;
using System.Linq.Expressions;

namespace Ayoos.Application.Patients;

internal static class PatientValidation
{
    public static void AddPatientRules<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> firstName,
        Expression<Func<T, string>> lastName,
        Expression<Func<T, string?>> preferredName,
        Expression<Func<T, DateOnly>> dateOfBirth,
        Expression<Func<T, PatientSex>> sex,
        Expression<Func<T, string>> email,
        Expression<Func<T, string>> phone,
        Expression<Func<T, PatientAddressModel>> address,
        Expression<Func<T, string?>> preferredLanguage,
        Expression<Func<T, EmergencyContactInput?>> emergencyContact)
    {
        validator.RuleFor(firstName).NotEmpty().MaximumLength(100).WithName("FirstName");
        validator.RuleFor(lastName).NotEmpty().MaximumLength(100).WithName("LastName");
        validator.RuleFor(preferredName).MaximumLength(100).WithName("PreferredName");
        validator.RuleFor(dateOfBirth)
            .Must(BeSaneDateOfBirth)
            .WithMessage(
                $"DateOfBirth must be in the past and no more than {Patient.MaximumAgeYears} years ago.")
            .WithName("DateOfBirth");
        validator.RuleFor(sex).IsInEnum().WithName("Sex");
        validator.RuleFor(email)
            .MaximumLength(320)
            .EmailAddress()
            .When(value => !string.IsNullOrWhiteSpace(GetValue(value, email)))
            .WithName("Email");
        validator.RuleFor(phone).MaximumLength(50).WithName("Phone");
        validator.RuleFor(value => value)
            .Must(value =>
                !string.IsNullOrWhiteSpace(GetValue(value, email))
                || !string.IsNullOrWhiteSpace(GetValue(value, phone)))
            .WithMessage("An email address or phone number is required.")
            .WithName("Contact");
        validator.RuleFor(address)
            .NotNull()
            .SetValidator(new PatientAddressModelValidator())
            .WithName("Address");
        validator.RuleFor(preferredLanguage)
            .MaximumLength(100)
            .WithName("PreferredLanguage");
        validator.RuleFor(emergencyContact)
            .SetValidator(new EmergencyContactInputValidator()!)
            .When(value => GetValue(value, emergencyContact) is not null)
            .WithName("EmergencyContact");
    }

    private static bool BeSaneDateOfBirth(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return dateOfBirth < today
            && dateOfBirth >= today.AddYears(-Patient.MaximumAgeYears);
    }

    private static TValue GetValue<T, TValue>(
        T instance,
        Expression<Func<T, TValue>> expression) =>
        expression.Compile()(instance);
}

internal sealed class PatientAddressModelValidator : AbstractValidator<PatientAddressModel>
{
    public PatientAddressModelValidator()
    {
        RuleFor(address => address.Line1).NotEmpty().MaximumLength(200);
        RuleFor(address => address.Line2).MaximumLength(200);
        RuleFor(address => address.City).NotEmpty().MaximumLength(100);
        RuleFor(address => address.State).NotEmpty().MaximumLength(100);
        RuleFor(address => address.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(address => address.Country).NotEmpty().MaximumLength(100);
    }
}

internal sealed class EmergencyContactInputValidator : AbstractValidator<EmergencyContactInput>
{
    public EmergencyContactInputValidator()
    {
        RuleFor(contact => contact.Name).NotEmpty().MaximumLength(200);
        RuleFor(contact => contact.Relationship).NotEmpty().MaximumLength(100);
        RuleFor(contact => contact.Phone).NotEmpty().MaximumLength(50);
    }
}
