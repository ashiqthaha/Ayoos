using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class ProviderTenantPersistenceTests
{
    [Fact]
    public async Task Availability_entities_persist_tenant_id_and_are_tenant_isolated()
    {
        var practice = CreatePractice("first-practice");
        var otherPractice = CreatePractice("other-practice");
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantId = practice.Id.ToString("D");
        Guid providerId;

        await using (var context = CreateContext(practice, databaseName, databaseRoot))
        {
            var provider = CreateProvider(practice.Id);
            providerId = provider.Id;
            var schedule = AvailabilitySchedule.Create(
                provider.Id,
                tenantId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30);
            var exception = AvailabilityException.Create(
                provider.Id,
                tenantId,
                new DateOnly(2026, 8, 10),
                AvailabilityExceptionType.Unavailable,
                null,
                null,
                "Conference");

            context.Practices.Add(practice);
            context.Providers.Add(provider);
            context.AvailabilitySchedules.Add(schedule);
            context.AvailabilityExceptions.Add(exception);
            await context.SaveChangesAsync();
        }

        await using (var sameTenant = CreateContext(
            practice,
            databaseName,
            databaseRoot))
        {
            Assert.NotNull(sameTenant.Model
                .FindEntityType(typeof(AvailabilitySchedule))?
                .FindProperty(nameof(AvailabilitySchedule.TenantId)));
            Assert.NotNull(sameTenant.Model
                .FindEntityType(typeof(AvailabilityException))?
                .FindProperty(nameof(AvailabilityException.TenantId)));

            var schedule = await sameTenant.AvailabilitySchedules.SingleAsync();
            var exception = await sameTenant.AvailabilityExceptions.SingleAsync();
            Assert.Equal(tenantId, schedule.TenantId);
            Assert.Equal(tenantId, exception.TenantId);
            Assert.Equal(providerId, schedule.ProviderId);
        }

        await using var otherTenant = CreateContext(
            otherPractice,
            databaseName,
            databaseRoot);
        Assert.Empty(await otherTenant.AvailabilitySchedules.ToListAsync());
        Assert.Empty(await otherTenant.AvailabilityExceptions.ToListAsync());
        Assert.Single(await otherTenant.AvailabilitySchedules
            .IgnoreQueryFilters()
            .ToListAsync());
    }

    private static Practice CreatePractice(string slug) =>
        Practice.Create(
            "Tenant Under Test",
            slug,
            "America/New_York",
            new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
            "practice@example.com",
            "+1-212-555-0100",
            DateTimeOffset.UtcNow);

    private static Provider CreateProvider(Guid practiceId) =>
        Provider.Create(
            practiceId,
            "Maya",
            "Patel",
            "MD",
            "Family Medicine",
            "maya@example.com",
            "+1-212-555-0101",
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

        return MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(
            tenant,
            options);
    }
}
