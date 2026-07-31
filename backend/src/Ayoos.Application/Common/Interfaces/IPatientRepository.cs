using Ayoos.Domain.Patients;

namespace Ayoos.Application.Common.Interfaces;

public sealed record PatientPage(
    IReadOnlyList<Patient> Items,
    int TotalCount);

public interface IPatientRepository
{
    Task<PatientPage> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Patient?> GetByIdAsync(
        Guid id,
        bool includeEmergencyContact = true,
        CancellationToken cancellationToken = default);

    Task<Patient?> GetByKeycloakUserIdAsync(
        string keycloakUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Patient>> FindActiveDuplicatesAsync(
        Guid practiceId,
        string lastName,
        DateOnly dateOfBirth,
        Guid? excludePatientId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsKeycloakUserLinkedAsync(
        string keycloakUserId,
        Guid excludePatientId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Patient patient,
        CancellationToken cancellationToken = default);

    Task AddEmergencyContactAsync(
        EmergencyContact emergencyContact,
        CancellationToken cancellationToken = default);

    void RemoveEmergencyContact(EmergencyContact emergencyContact);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
