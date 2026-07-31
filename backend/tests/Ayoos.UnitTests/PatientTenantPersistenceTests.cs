using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class PatientTenantPersistenceTests
{
    [Fact]
    public async Task Patient_and_emergency_contact_receive_tenant_id_when_added()
    {
        var practice = CreatePractice();
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantId = practice.Id.ToString("D");

        await using (var context = CreateContext(
            practice,
            databaseName,
            databaseRoot))
        {
            var patient = Patient.Create(
                practice.Id,
                "Amina",
                "Rahman",
                "Mina",
                new DateOnly(1991, 4, 12),
                PatientSex.Female,
                "amina@example.com",
                "",
                new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
                "English",
                DateTimeOffset.UtcNow);
            patient.ReplaceEmergencyContact(
                EmergencyContact.Create(
                    patient.Id,
                    "Karim Rahman",
                    "Spouse",
                    "+1-212-555-0199"));

            context.Practices.Add(practice);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext(
            practice,
            databaseName,
            databaseRoot);
        Assert.NotNull(
            readContext.Model.FindEntityType(typeof(Patient))?.FindProperty("TenantId"));
        Assert.NotNull(
            readContext.Model.FindEntityType(typeof(EmergencyContact))?.FindProperty("TenantId"));

        var patientTenantId = await readContext.Patients
            .IgnoreQueryFilters()
            .Select(patient => EF.Property<string>(patient, "TenantId"))
            .SingleAsync();
        var contactTenantId = await readContext.EmergencyContacts
            .IgnoreQueryFilters()
            .Select(contact => EF.Property<string>(contact, "TenantId"))
            .SingleAsync();

        Assert.Equal(tenantId, patientTenantId);
        Assert.Equal(tenantId, contactTenantId);
    }

    private static AyoosDbContext CreateContext(
        Practice practice,
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<AyoosDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;
        var tenant = new TenantInfo
        {
            Id = practice.Id.ToString("D"),
            Identifier = practice.Slug,
            Name = practice.Name
        };

        return MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(tenant, options);
    }

    private static Practice CreatePractice() =>
        Practice.Create(
            "Tenant Under Test",
            "tenant-under-test",
            "America/New_York",
            new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
            "practice@example.com",
            "+1-212-555-0100",
            DateTimeOffset.UtcNow);
}
