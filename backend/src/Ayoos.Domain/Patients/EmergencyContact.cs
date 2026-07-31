using Ayoos.Domain.Common;

namespace Ayoos.Domain.Patients;

public sealed class EmergencyContact : Entity
{
    private EmergencyContact()
        : base(Guid.NewGuid())
    {
    }

    private EmergencyContact(
        Guid id,
        Guid patientId,
        string name,
        string relationship,
        string phone)
        : base(id)
    {
        PatientId = patientId;
        Name = name;
        Relationship = relationship;
        Phone = phone;
    }

    public Guid PatientId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Relationship { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public static EmergencyContact Create(
        Guid patientId,
        string name,
        string relationship,
        string phone)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(patientId, Guid.Empty);

        return new EmergencyContact(
            Guid.NewGuid(),
            patientId,
            Required(name, nameof(name)),
            Required(relationship, nameof(relationship)),
            Required(phone, nameof(phone)));
    }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
