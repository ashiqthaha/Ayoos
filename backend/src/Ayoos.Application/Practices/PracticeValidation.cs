using FluentValidation;

namespace Ayoos.Application.Practices;

internal static class PracticeValidation
{
    public const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    public static bool IsValidTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out _))
            {
                return false;
            }

            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}

internal sealed class PracticeAddressModelValidator : AbstractValidator<PracticeAddressModel>
{
    public PracticeAddressModelValidator()
    {
        RuleFor(address => address.Line1).NotEmpty().MaximumLength(200);
        RuleFor(address => address.Line2).MaximumLength(200);
        RuleFor(address => address.City).NotEmpty().MaximumLength(100);
        RuleFor(address => address.State).NotEmpty().MaximumLength(100);
        RuleFor(address => address.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(address => address.Country).NotEmpty().MaximumLength(100);
    }
}
