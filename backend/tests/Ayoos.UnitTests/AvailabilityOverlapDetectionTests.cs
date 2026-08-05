using Ayoos.Application.Providers.CreateAvailabilitySchedule;
using Ayoos.Application.Providers.UpdateAvailabilitySchedule;
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
    private static readonly Guid ProviderId = Guid.NewGuid();
    private const string TenantId = "f993c686-f775-44e5-a673-ea23cb00408b";

    [Fact]
    public void Detector_returns_an_overlapping_window()
    {
        var existing = CreateSchedule(new TimeOnly(9, 0), new TimeOnly(12, 0));

        var conflicts = AvailabilityOverlapDetector.FindConflicts(
            [existing],
            ProviderId,
            DayOfWeek.Monday,
            new TimeOnly(11, 30),
            new TimeOnly(13, 0));

        Assert.Collection(conflicts, conflict => Assert.Equal(existing.Id, conflict.Id));
    }

    [Fact]
    public void Detector_does_not_return_an_adjacent_window()
    {
        var existing = CreateSchedule(new TimeOnly(9, 0), new TimeOnly(12, 0));

        var conflicts = AvailabilityOverlapDetector.FindConflicts(
            [existing],
            ProviderId,
            DayOfWeek.Monday,
            new TimeOnly(12, 0),
            new TimeOnly(13, 0));

        Assert.Empty(conflicts);
    }

    [Fact]
    public void Detector_does_not_return_a_disjoint_window()
    {
        var existing = CreateSchedule(new TimeOnly(9, 0), new TimeOnly(12, 0));

        var conflicts = AvailabilityOverlapDetector.FindConflicts(
            [existing],
            ProviderId,
            DayOfWeek.Monday,
            new TimeOnly(14, 0),
            new TimeOnly(16, 0));

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task Create_previews_overlap_without_persisting_until_confirmed()
    {
        var fixture = await Fixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var handler = new CreateAvailabilityScheduleCommandHandler(
            new ProviderRepository(context),
            TimeProvider.System);
        var command = new CreateAvailabilityScheduleCommand(
            fixture.ProviderId,
            DayOfWeek.Monday,
            new TimeOnly(11, 30),
            new TimeOnly(15, 0),
            30);

        var preview = await handler.Handle(command, CancellationToken.None);

        Assert.Null(preview.Schedule);
        Assert.True(preview.OverlapPreview.HasConflicts);
        Assert.Single(preview.OverlapPreview.Conflicts);
        Assert.Single(await context.AvailabilitySchedules.ToListAsync());

        var confirmed = await handler.Handle(
            command with { ConfirmOverlap = true },
            CancellationToken.None);

        Assert.NotNull(confirmed.Schedule);
        Assert.True(confirmed.OverlapPreview.HasConflicts);
        Assert.Equal(2, await context.AvailabilitySchedules.CountAsync());
    }

    [Fact]
    public async Task Update_excludes_itself_from_the_overlap_preview()
    {
        var fixture = await Fixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var handler = new UpdateAvailabilityScheduleCommandHandler(
            new ProviderRepository(context),
            TimeProvider.System);

        var result = await handler.Handle(
            new UpdateAvailabilityScheduleCommand(
                fixture.ProviderId,
                fixture.ScheduleId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30),
            CancellationToken.None);

        Assert.NotNull(result.Schedule);
        Assert.False(result.OverlapPreview.HasConflicts);
    }

    private static AvailabilitySchedule CreateSchedule(TimeOnly start, TimeOnly end) =>
        AvailabilitySchedule.Create(
            ProviderId,
            TenantId,
            DayOfWeek.Monday,
            start,
            end,
            30);

    private sealed class Fixture(
        Practice practice,
        string databaseName,
        InMemoryDatabaseRoot databaseRoot,
        Guid providerId,
        Guid scheduleId)
    {
        public Guid ProviderId { get; } = providerId;
        public Guid ScheduleId { get; } = scheduleId;

        public AyoosDbContext CreateContext()
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

        public static async Task<Fixture> CreateAsync()
        {
            var practice = Practice.Create(
                "Tenant Under Test",
                "tenant-under-test",
                "America/New_York",
                new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
                "practice@example.com",
                "+1-212-555-0100",
                DateTimeOffset.UtcNow);
            var databaseName = Guid.NewGuid().ToString("N");
            var databaseRoot = new InMemoryDatabaseRoot();
            var seed = new Fixture(
                practice,
                databaseName,
                databaseRoot,
                Guid.Empty,
                Guid.Empty);

            await using var context = seed.CreateContext();
            var provider = Provider.Create(
                practice.Id,
                "Maya",
                "Patel",
                "MD",
                "Family Medicine",
                "maya@example.com",
                "+1-212-555-0101",
                DateTimeOffset.UtcNow);
            var schedule = AvailabilitySchedule.Create(
                provider.Id,
                practice.Id.ToString("D"),
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30);
            context.Practices.Add(practice);
            context.Providers.Add(provider);
            context.AvailabilitySchedules.Add(schedule);
            await context.SaveChangesAsync();

            return new Fixture(
                practice,
                databaseName,
                databaseRoot,
                provider.Id,
                schedule.Id);
        }
    }
}
