using Ayoos.Domain.Common;

namespace Ayoos.Domain.Practices;

public sealed record Address : ValueObject
{
    private Address()
    {
    }

    public Address(
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country)
    {
        Line1 = Required(line1, nameof(line1));
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = Required(city, nameof(city));
        State = Required(state, nameof(state));
        PostalCode = Required(postalCode, nameof(postalCode));
        Country = Required(country, nameof(country));
    }

    public string Line1 { get; private init; } = string.Empty;

    public string? Line2 { get; private init; }

    public string City { get; private init; } = string.Empty;

    public string State { get; private init; } = string.Empty;

    public string PostalCode { get; private init; } = string.Empty;

    public string Country { get; private init; } = string.Empty;

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
