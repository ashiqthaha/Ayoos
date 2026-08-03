using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Providers.CreateAvailability;
using Ayoos.Application.Providers.UpdateAvailability;
using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class AvailabilityOverlapDetectionTests
{
    [Fact]
    public async Task Create_rejects_overlap_and_does_not_persist_it()
    {
        var fixture = await Fixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var handler = new CreateAvailabilityCommandHandler(
            new ProviderRepository(context));

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CreateAvailabilityCommand(
                fixture.ProviderId,
                DayOfWeek.Monday,
                new TimeOnly(11, 30),
                new TimeOnly(15, 0),
                30),
            CancellationToken.None));

        Assert.Single(await context.AvailabilitySchedules.ToListAsync());
    }

    [Fact]
    public async Task Create_accepts_an_adjacent_schedule()
    {
        var fixture = await Fixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var handler = new CreateAvailabilityCommandHandler(
            new ProviderRepository(context));

        await handler.Handle(
            new CreateAvailabilityCommand(
                fixture.ProviderId,
                DayOfWeek.Monday,
                new TimeOnly(12, 0),
                new TimeOnly(15, 0),
                30),
            CancellationToken.None);

        Assert.Equal(2, await context.AvailabilitySchedules.CountAsync());
    }

    [Fact]
    public async Task Update_excludes_itself_but_rejects_overlap_with_another_schedule()
    {
        var fixture = await Fixture.CreateAsync(addSecondSchedule: true);
        await using var context = fixture.CreateContext();
        var handler = new UpdateAvailabilityCommandHandler(
            new ProviderRepository(context));

        var unchanged = await handler.Handle(
            new UpdateAvailabilityCommand(
                fixture.ProviderId,
                fixture.ScheduleId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30),
            CancellationToken.None);
        Assert.Equal(fixture.ScheduleId, unchanged.Id);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateAvailabilityCommand(
                fixture.ProviderId,
                fixture.ScheduleId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(13, 0),
                30),
            CancellationToken.None));
    }

    private sealed class Fixture(
        Practice practice,
        string databaseName,
        InMemoryDatabaseRoot databaseRoot,
        Guid providerId,
        Guid scheduleId)
    {
        private string DatabaseName { get; } = databaseName;
        private InMemoryDatabaseRoot DatabaseRoot { get; } = databaseRoot;
        public Guid ProviderId { get; } = providerId;
        public Guid ScheduleId { get; } = scheduleId;

        public AyoosDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AyoosDbContext>()
                .UseInMemoryDatabase(DatabaseName, DatabaseRoot)
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

        public static async Task<Fixture> CreateAsync(bool addSecondSchedule = false)
        {
            var practice = Practice.Create(
                "Tenant Under Test",
                "tenant-under-test",
                "America/New_York",
                new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
                "practice@example.com",
                "+1-212-555-0100",
                DateTimeOffset.UtcNow);
            var fixture = new Fixture(
                practice,
                Guid.NewGuid().ToString("N"),
                new InMemoryDatabaseRoot(),
                Guid.Empty,
                Guid.Empty);

            await using var context = fixture.CreateContext();
            var provider = Provider.Create(
                practice.Id,
                "Maya",
                "Patel",
                "MD",
                "Family Medicine",
                "maya@example.com",
                "+1-212-555-0101",
                DateTimeOffset.UtcNow);
            var first = AvailabilitySchedule.Create(
                provider.Id,
                practice.Id.ToString("D"),
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30);

            context.Practices.Add(practice);
            context.Providers.Add(provider);
            context.AvailabilitySchedules.Add(first);
            if (addSecondSchedule)
            {
                context.AvailabilitySchedules.Add(AvailabilitySchedule.Create(
                    provider.Id,
                    practice.Id.ToString("D"),
                    DayOfWeek.Monday,
                    new TimeOnly(12, 0),
                    new TimeOnly(15, 0),
                    30));
            }

            await context.SaveChangesAsync();
            return new Fixture(
                practice,
                fixture.DatabaseName,
                fixture.DatabaseRoot,
                provider.Id,
                first.Id);
        }
    }
}
