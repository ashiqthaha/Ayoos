using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Patients;
using Ayoos.Application.Patients.RegisterPatient;
using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class PatientDuplicateDetectionTests
{
    [Fact]
    public async Task Returns_warning_and_does_not_persist_until_confirmed()
    {
        var practice = CreatePractice();
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();

        await using (var seedContext = CreateContext(practice, databaseName, databaseRoot))
        {
            var existing = Patient.Create(
                practice.Id,
                "Existing",
                "Rahman",
                null,
                new DateOnly(1991, 4, 12),
                PatientSex.Female,
                "existing@example.com",
                "",
                new Address("1 First Avenue", null, "New York", "NY", "10001", "US"),
                null,
                DateTimeOffset.UtcNow);

            seedContext.Practices.Add(practice);
            seedContext.Patients.Add(existing);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(practice, databaseName, databaseRoot);
        var handler = new RegisterPatientCommandHandler(
            new PatientRepository(context),
            new TestPracticeContext(practice.Id));
        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.DuplicateWarning);
        Assert.Null(result.Patient);
        Assert.Single(result.PossibleMatches);
        Assert.Equal(1, await context.Patients.CountAsync());

        var confirmed = await handler.Handle(
            Command() with { ConfirmDuplicate = true },
            CancellationToken.None);

        Assert.True(confirmed.DuplicateWarning);
        Assert.NotNull(confirmed.Patient);
        Assert.Equal(2, await context.Patients.CountAsync());
    }

    private static RegisterPatientCommand Command() =>
        new(
            "Amina",
            "rahman",
            null,
            new DateOnly(1991, 4, 12),
            PatientSex.Female,
            "amina@example.com",
            "",
            new PatientAddressModel(
                "100 Main Street",
                null,
                "New York",
                "NY",
                "10001",
                "US"),
            null,
            null);

    private static Practice CreatePractice() =>
        Practice.Create(
            "Tenant Under Test",
            "tenant-under-test",
            "America/New_York",
            new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
            "practice@example.com",
            "+1-212-555-0100",
            DateTimeOffset.UtcNow);

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

    private sealed class TestPracticeContext(Guid practiceId) : ICurrentPracticeContext
    {
        public Guid PracticeId { get; } = practiceId;
    }
}
