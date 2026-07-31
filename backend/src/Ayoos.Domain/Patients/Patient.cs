using Ayoos.Domain.Common;
using Ayoos.Domain.Practices;

namespace Ayoos.Domain.Patients;

public sealed class Patient : Entity
{
    public const int MaximumAgeYears = 130;

    private Patient()
        : base(Guid.NewGuid())
    {
    }

    private Patient(
        Guid id,
        Guid practiceId,
        string firstName,
        string lastName,
        string? preferredName,
        DateOnly dateOfBirth,
        PatientSex sex,
        string email,
        string phone,
        Address address,
        string? preferredLanguage,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PracticeId = practiceId;
        FirstName = firstName;
        LastName = lastName;
        PreferredName = preferredName;
        DateOfBirth = dateOfBirth;
        Sex = sex;
        Email = email;
        Phone = phone;
        Address = address;
        PreferredLanguage = preferredLanguage;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid PracticeId { get; private set; }

    public string? KeycloakUserId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? PreferredName { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public PatientSex Sex { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;

    public string? PreferredLanguage { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public EmergencyContact? EmergencyContact { get; private set; }

    public static Patient Create(
        Guid practiceId,
        string firstName,
        string lastName,
        string? preferredName,
        DateOnly dateOfBirth,
        PatientSex sex,
        string email,
        string phone,
        Address address,
        string? preferredLanguage,
        DateTimeOffset createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(practiceId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(address);
        ValidateDateOfBirth(dateOfBirth, createdAtUtc);
        ValidateContactChannels(email, phone);

        return new Patient(
            Guid.NewGuid(),
            practiceId,
            Required(firstName, nameof(firstName)),
            Required(lastName, nameof(lastName)),
            Optional(preferredName),
            dateOfBirth,
            sex,
            Optional(email) ?? string.Empty,
            Optional(phone) ?? string.Empty,
            address,
            Optional(preferredLanguage),
            createdAtUtc.ToUniversalTime());
    }

    public void ReplaceEmergencyContact(EmergencyContact? emergencyContact)
    {
        if (emergencyContact is not null && emergencyContact.PatientId != Id)
        {
            throw new ArgumentException(
                "The emergency contact must belong to this patient.",
                nameof(emergencyContact));
        }

        EmergencyContact = emergencyContact;
    }

    public void Update(
        string firstName,
        string lastName,
        string? preferredName,
        DateOnly dateOfBirth,
        PatientSex sex,
        string email,
        string phone,
        Address address,
        string? preferredLanguage,
        EmergencyContact? emergencyContact,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(address);
        ValidateDateOfBirth(dateOfBirth, updatedAtUtc);
        ValidateContactChannels(email, phone);

        FirstName = Required(firstName, nameof(firstName));
        LastName = Required(lastName, nameof(lastName));
        PreferredName = Optional(preferredName);
        DateOfBirth = dateOfBirth;
        Sex = sex;
        Email = Optional(email) ?? string.Empty;
        Phone = Optional(phone) ?? string.Empty;
        Address = address;
        PreferredLanguage = Optional(preferredLanguage);
        ReplaceEmergencyContact(emergencyContact);
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    public void LinkToKeycloakUser(
        string keycloakUserId,
        DateTimeOffset updatedAtUtc)
    {
        KeycloakUserId = Required(keycloakUserId, nameof(keycloakUserId));
        UpdatedAtUtc = updatedAtUtc.ToUniversalTime();
    }

    private static void ValidateDateOfBirth(
        DateOnly dateOfBirth,
        DateTimeOffset referenceTime)
    {
        var today = DateOnly.FromDateTime(referenceTime.UtcDateTime);
        if (dateOfBirth >= today || dateOfBirth < today.AddYears(-MaximumAgeYears))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateOfBirth),
                $"Date of birth must be in the past and no more than {MaximumAgeYears} years ago.");
        }
    }

    private static void ValidateContactChannels(string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("An email address or phone number is required.");
        }
    }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
