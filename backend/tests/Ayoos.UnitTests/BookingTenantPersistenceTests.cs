using Ayoos.Domain.Bookings;
using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class BookingTenantPersistenceTests
{
    [Fact]
    public async Task Booking_persists_tenant_id_and_is_tenant_isolated()
    {
        var practice = CreatePractice("booking-tenant");
        var otherPractice = CreatePractice("other-booking-tenant");
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();
        var tenantId = practice.Id.ToString("D");
        Guid bookingId;

        await using (var context = CreateContext(
            practice,
            databaseName,
            databaseRoot))
        {
            var provider = CreateProvider(practice.Id);
            var patient = CreatePatient(practice.Id);
            var schedule = AvailabilitySchedule.Create(
                provider.Id,
                tenantId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(10, 0),
                30);
            var start = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero);
            var booking = Booking.Create(
                tenantId,
                patient.Id,
                provider.Id,
                schedule.Id,
                start,
                start.AddMinutes(30),
                "Tenant persistence",
                start.AddDays(-1));
            bookingId = booking.Id;

            context.Practices.Add(practice);
            context.Providers.Add(provider);
            context.Patients.Add(patient);
            context.AvailabilitySchedules.Add(schedule);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();
        }

        await using (var sameTenant = CreateContext(
            practice,
            databaseName,
            databaseRoot))
        {
            var booking = await sameTenant.Bookings.SingleAsync();
            Assert.Equal(bookingId, booking.Id);
            Assert.Equal(tenantId, booking.TenantId);
            Assert.NotNull(sameTenant.Model
                .FindEntityType(typeof(Booking))?
                .FindProperty(nameof(Booking.TenantId)));
            var bookingType = sameTenant.Model.FindEntityType(typeof(Booking));
            Assert.True(bookingType?
                .FindProperty(nameof(Booking.RowVersion))?
                .IsConcurrencyToken);
            var slotIndex = Assert.Single(bookingType!.GetIndexes(), index =>
                index.Properties.Select(property => property.Name).SequenceEqual(
                    [nameof(Booking.TenantId), nameof(Booking.ProviderId), nameof(Booking.ScheduledStart)]));
            Assert.True(slotIndex.IsUnique);
            Assert.Equal(
                "\"Status\" IN ('Pending', 'Confirmed')",
                slotIndex.GetFilter());
        }

        await using var otherTenant = CreateContext(
            otherPractice,
            databaseName,
            databaseRoot);
        Assert.Empty(await otherTenant.Bookings.ToListAsync());
        Assert.Single(await otherTenant.Bookings.IgnoreQueryFilters().ToListAsync());
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

    private static Patient CreatePatient(Guid practiceId) =>
        Patient.Create(
            practiceId,
            "Jordan",
            "Lee",
            null,
            new DateOnly(1990, 1, 1),
            PatientSex.Unknown,
            "jordan@example.com",
            "+1-212-555-0102",
            new Address("101 Main Street", null, "New York", "NY", "10001", "US"),
            "English",
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
