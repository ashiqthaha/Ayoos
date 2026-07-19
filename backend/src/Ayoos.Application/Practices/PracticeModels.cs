using Ayoos.Domain.Practices;

namespace Ayoos.Application.Practices;

public sealed record PracticeAddressModel(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country)
{
    public Address ToValueObject() =>
        new(Line1, Line2, City, State, PostalCode, Country);
}

public sealed record PracticeModel(
    Guid Id,
    string Name,
    string Slug,
    string TimeZone,
    PracticeAddressModel Address,
    string ContactEmail,
    string ContactPhone,
    DateTimeOffset CreatedAtUtc,
    bool IsActive);

internal static class PracticeMappings
{
    public static PracticeModel ToModel(this Practice practice) =>
        new(
            practice.Id,
            practice.Name,
            practice.Slug,
            practice.TimeZone,
            new PracticeAddressModel(
                practice.Address.Line1,
                practice.Address.Line2,
                practice.Address.City,
                practice.Address.State,
                practice.Address.PostalCode,
                practice.Address.Country),
            practice.ContactEmail,
            practice.ContactPhone,
            practice.CreatedAtUtc,
            practice.IsActive);
}
