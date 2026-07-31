using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Patients;
using Ayoos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Repositories;

internal sealed class PatientRepository(AyoosDbContext dbContext)
    : IPatientRepository
{
    public async Task<PatientPage> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Patient> query = dbContext.Patients
            .Include(patient => patient.EmergencyContact);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(patient =>
                patient.FirstName.ToLower().Contains(term)
                || patient.LastName.ToLower().Contains(term)
                || (patient.PreferredName != null
                    && patient.PreferredName.ToLower().Contains(term))
                || patient.Email.ToLower().Contains(term)
                || patient.Phone.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var patients = await query
            .OrderBy(patient => patient.LastName)
            .ThenBy(patient => patient.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PatientPage(patients, totalCount);
    }

    public Task<Patient?> GetByIdAsync(
        Guid id,
        bool includeEmergencyContact = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Patient> query = dbContext.Patients;
        if (includeEmergencyContact)
        {
            query = query.Include(patient => patient.EmergencyContact);
        }

        return query.SingleOrDefaultAsync(patient => patient.Id == id, cancellationToken);
    }

    public Task<Patient?> GetByKeycloakUserIdAsync(
        string keycloakUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.Patients
            .Include(patient => patient.EmergencyContact)
            .SingleOrDefaultAsync(
                patient => patient.KeycloakUserId == keycloakUserId,
                cancellationToken);

    public async Task<IReadOnlyList<Patient>> FindActiveDuplicatesAsync(
        Guid practiceId,
        string lastName,
        DateOnly dateOfBirth,
        Guid? excludePatientId = null,
        CancellationToken cancellationToken = default) =>
        await dbContext.Patients
            .Where(patient =>
                patient.PracticeId == practiceId
                && patient.IsActive
                && patient.LastName.ToLower() == lastName.ToLower()
                && patient.DateOfBirth == dateOfBirth
                && (!excludePatientId.HasValue || patient.Id != excludePatientId.Value))
            .OrderBy(patient => patient.FirstName)
            .ToListAsync(cancellationToken);

    public Task<bool> IsKeycloakUserLinkedAsync(
        string keycloakUserId,
        Guid excludePatientId,
        CancellationToken cancellationToken = default) =>
        dbContext.Patients.AnyAsync(
            patient =>
                patient.KeycloakUserId == keycloakUserId
                && patient.Id != excludePatientId,
            cancellationToken);

    public async Task AddAsync(
        Patient patient,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Patients.AddAsync(patient, cancellationToken);
    }

    public async Task AddEmergencyContactAsync(
        EmergencyContact emergencyContact,
        CancellationToken cancellationToken = default)
    {
        await dbContext.EmergencyContacts.AddAsync(emergencyContact, cancellationToken);
    }

    public void RemoveEmergencyContact(EmergencyContact emergencyContact)
    {
        dbContext.EmergencyContacts.Remove(emergencyContact);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
