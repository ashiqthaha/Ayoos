using Ayoos.Application.Patients;
using Ayoos.Application.Patients.RegisterPatient;
using Ayoos.Domain.Patients;

namespace Ayoos.UnitTests;

public sealed class RegisterPatientCommandValidatorTests
{
    private readonly RegisterPatientCommandValidator _validator = new();

    [Fact]
    public async Task Rejects_future_date_of_birth()
    {
        var command = ValidCommand() with
        {
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)
        };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(command.DateOfBirth));
    }

    [Fact]
    public async Task Rejects_patient_without_contact_channel()
    {
        var command = ValidCommand() with { Email = "", Phone = "" };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == "Contact"
                && error.ErrorMessage.Contains("email address or phone number"));
    }

    [Fact]
    public async Task Accepts_valid_command()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        Assert.True(result.IsValid);
    }

    private static RegisterPatientCommand ValidCommand() =>
        new(
            "Amina",
            "Rahman",
            "Mina",
            new DateOnly(1991, 4, 12),
            PatientSex.Female,
            "amina@example.com",
            "",
            new PatientAddressModel(
                "100 Main Street",
                "Apt 4B",
                "New York",
                "NY",
                "10001",
                "US"),
            "English",
            new EmergencyContactInput("Karim Rahman", "Spouse", "+1-212-555-0199"));
}
