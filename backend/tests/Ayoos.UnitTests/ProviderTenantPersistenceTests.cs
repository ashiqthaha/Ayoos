using Ayoos.Application.Providers;
using Ayoos.Application.Providers.AddAvailabilityException;
using Ayoos.Application.Providers.RemoveAvailabilityException;
using Ayoos.Application.Providers.SetAvailabilityRules;
using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class ProviderTenantPersistenceTests
{
    private readonly Practice _practice = CreatePractice();
    private readonly string _databaseName = Guid.NewGuid().ToString("N");
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    [Fact]
    public async Task Setting_rules_twice_replaces_tenant_scoped_entities()
    {
        var providerId = await SeedProviderAsync();
        var effectiveFrom = new DateOnly(2026, 8, 1);

        await SetRulesAsync(
            providerId,
            [
                Rule(DayOfWeek.Monday, 9, 12, effectiveFrom),
                Rule(DayOfWeek.Tuesday, 10, 14, effectiveFrom)
            ]);

        await using (var firstRead = CreateContext())
        {
            AssertMultiTenantMetadata(firstRead);
            var stored = await ReadRulesWithTenantIdAsync(firstRead, providerId);

            Assert.Equal(2, stored.Count);
            Assert.All(stored, row => Assert.Equal(TenantId, row.TenantId));
        }

        await SetRulesAsync(
            providerId,
            [
                Rule(DayOfWeek.Wednesday, 8, 11, effectiveFrom),
                Rule(DayOfWeek.Friday, 13, 17, effectiveFrom)
            ]);

        await using var secondRead = CreateContext();
        var replacements = await ReadRulesWithTenantIdAsync(secondRead, providerId);

        Assert.Collection(
            replacements.OrderBy(row => row.Rule.DayOfWeek),
            row =>
            {
                Assert.Equal(DayOfWeek.Wednesday, row.Rule.DayOfWeek);
                Assert.Equal(TenantId, row.TenantId);
            },
            row =>
            {
                Assert.Equal(DayOfWeek.Friday, row.Rule.DayOfWeek);
                Assert.Equal(TenantId, row.TenantId);
            });
    }

    [Fact]
    public async Task Adding_and_removing_exception_preserves_tenant_enforcement()
    {
        var providerId = await SeedProviderAsync();
        var date = new DateOnly(2026, 8, 7);
        Guid exceptionId;

        await using (var addContext = CreateContext())
        {
            var handler = new AddAvailabilityExceptionCommandHandler(
                new ProviderRepository(addContext));
            var result = await handler.Handle(
                new AddAvailabilityExceptionCommand(
                    providerId,
                    date,
                    true,
                    null,
                    null,
                    "Conference"),
                CancellationToken.None);
            exceptionId = result.Id;
        }

        await using (var readContext = CreateContext())
        {
            var stored = await readContext.AvailabilityExceptions
                .IgnoreQueryFilters()
                .Where(item => item.ProviderId == providerId)
                .Select(item => new
                {
                    Exception = item,
                    TenantId = EF.Property<string>(item, "TenantId")
                })
                .SingleAsync();

            Assert.Equal(exceptionId, stored.Exception.Id);
            Assert.Equal(TenantId, stored.TenantId);
        }

        await using (var removeContext = CreateContext())
        {
            var handler = new RemoveAvailabilityExceptionCommandHandler(
                new ProviderRepository(removeContext));
            await handler.Handle(
                new RemoveAvailabilityExceptionCommand(providerId, exceptionId),
                CancellationToken.None);
        }

        await using var finalContext = CreateContext();
        Assert.Empty(
            await finalContext.AvailabilityExceptions
                .IgnoreQueryFilters()
                .Where(item => item.ProviderId == providerId)
                .ToListAsync());
    }

    private Guid PracticeId => _practice.Id;

    private string TenantId => PracticeId.ToString("D");

    private AyoosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AyoosDbContext>()
            .UseInMemoryDatabase(_databaseName, _databaseRoot)
            .Options;
        var tenant = new TenantInfo
        {
            Id = TenantId,
            Identifier = "tenant-under-test",
            Name = "Tenant Under Test"
        };

        return MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(
            tenant,
            options);
    }

    private async Task<Guid> SeedProviderAsync()
    {
        await using var context = CreateContext();
        var provider = Provider.Create(
            PracticeId,
            "Maya",
            "Patel",
            "MD",
            "Family Medicine",
            "maya@example.com",
            "+1-212-555-0101",
            DateTimeOffset.UtcNow);

        context.Practices.Add(_practice);
        context.Providers.Add(provider);
        await context.SaveChangesAsync();

        return provider.Id;
    }

    private async Task SetRulesAsync(
        Guid providerId,
        IReadOnlyList<AvailabilityRuleInput> rules)
    {
        await using var context = CreateContext();
        var handler = new SetAvailabilityRulesCommandHandler(
            new ProviderRepository(context));

        await handler.Handle(
            new SetAvailabilityRulesCommand(providerId, rules),
            CancellationToken.None);
    }

    private static AvailabilityRuleInput Rule(
        DayOfWeek day,
        int startHour,
        int endHour,
        DateOnly effectiveFrom) =>
        new(
            day,
            new TimeOnly(startHour, 0),
            new TimeOnly(endHour, 0),
            30,
            effectiveFrom,
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

    private static async Task<List<StoredRule>> ReadRulesWithTenantIdAsync(
        AyoosDbContext context,
        Guid providerId) =>
        await context.AvailabilityRules
            .IgnoreQueryFilters()
            .Where(rule => rule.ProviderId == providerId)
            .Select(rule => new StoredRule(
                rule,
                EF.Property<string>(rule, "TenantId")))
            .ToListAsync();

    private static void AssertMultiTenantMetadata(AyoosDbContext context)
    {
        Assert.NotNull(
            context.Model.FindEntityType(typeof(AvailabilityRule))?
                .FindProperty("TenantId"));
        Assert.NotNull(
            context.Model.FindEntityType(typeof(AvailabilityException))?
                .FindProperty("TenantId"));
    }

    private sealed record StoredRule(AvailabilityRule Rule, string TenantId);
}
