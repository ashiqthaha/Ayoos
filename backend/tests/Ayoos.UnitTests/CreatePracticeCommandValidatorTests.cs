using Ayoos.Application.Practices;
using Ayoos.Application.Practices.CreatePractice;

namespace Ayoos.UnitTests;

public sealed class CreatePracticeCommandValidatorTests
{
    private readonly CreatePracticeCommandValidator _validator = new();

    [Fact]
    public async Task Accepts_valid_command()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_name_shorter_than_two_characters()
    {
        var command = ValidCommand() with { Name = "A" };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.Name));
    }

    [Fact]
    public async Task Rejects_slug_that_is_not_lowercase_kebab_case()
    {
        var command = ValidCommand() with { Slug = "Downtown Clinic" };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.Slug));
    }

    [Fact]
    public async Task Rejects_invalid_time_zone_and_email()
    {
        var command = ValidCommand() with
        {
            TimeZone = "Mars/Olympus_Mons",
            ContactEmail = "not-an-email"
        };

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.TimeZone));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.ContactEmail));
    }

    private static CreatePracticeCommand ValidCommand() =>
        new(
            "Downtown Family Clinic",
            "downtown-family-clinic",
            "America/New_York",
            new PracticeAddressModel(
                "100 Main Street",
                "Suite 200",
                "New York",
                "NY",
                "10001",
                "US"),
            "hello@downtownclinic.example",
            "+1-212-555-0100");
}
