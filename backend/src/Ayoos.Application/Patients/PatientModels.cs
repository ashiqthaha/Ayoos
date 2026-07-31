using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;

namespace Ayoos.Application.Patients;

public sealed record PatientAddressModel(
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

public sealed record EmergencyContactInput(
    string Name,
    string Relationship,
    string Phone)
{
    public EmergencyContact ToEntity(Guid patientId) =>
        EmergencyContact.Create(patientId, Name, Relationship, Phone);
}

public sealed record EmergencyContactModel(
    Guid Id,
    string Name,
    string Relationship,
    string Phone);

public sealed record PatientModel(
    Guid Id,
    Guid PracticeId,
    string? KeycloakUserId,
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly DateOfBirth,
    PatientSex Sex,
    string Email,
    string Phone,
    PatientAddressModel Address,
    string? PreferredLanguage,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    EmergencyContactModel? EmergencyContact);

public sealed record PatientDuplicateMatchModel(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string Phone);

public sealed record RegisterPatientResult(
    PatientModel? Patient,
    bool DuplicateWarning,
    IReadOnlyList<PatientDuplicateMatchModel> PossibleMatches);

public sealed record PagedPatientListModel(
    IReadOnlyList<PatientModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

internal static class PatientMappings
{
    public static PatientModel ToModel(this Patient patient) =>
        new(
            patient.Id,
            patient.PracticeId,
            patient.KeycloakUserId,
            patient.FirstName,
            patient.LastName,
            patient.PreferredName,
            patient.DateOfBirth,
            patient.Sex,
            patient.Email,
            patient.Phone,
            new PatientAddressModel(
                patient.Address.Line1,
                patient.Address.Line2,
                patient.Address.City,
                patient.Address.State,
                patient.Address.PostalCode,
                patient.Address.Country),
            patient.PreferredLanguage,
            patient.IsActive,
            patient.CreatedAtUtc,
            patient.UpdatedAtUtc,
            patient.EmergencyContact?.ToModel());

    public static PatientDuplicateMatchModel ToDuplicateModel(this Patient patient) =>
        new(
            patient.Id,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Email,
            patient.Phone);

    private static EmergencyContactModel ToModel(this EmergencyContact contact) =>
        new(contact.Id, contact.Name, contact.Relationship, contact.Phone);
}
