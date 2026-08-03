using Ayoos.Application;
using Ayoos.Application.Bookings.CreateBooking;
using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ayoos.UnitTests;

public sealed class BookingAvailabilityTests
{
    [Fact]
    public async Task Create_rejects_a_time_that_is_not_a_computed_slot()
    {
        await using var fixture = await Fixture.CreateAsync();
        var sender = fixture.Services.GetRequiredService<ISender>();
        var start = new DateTimeOffset(2026, 8, 3, 9, 15, 0, TimeSpan.Zero);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            sender.Send(new CreateBookingCommand(
                fixture.PatientId,
                fixture.ProviderId,
                fixture.ScheduleId,
                start,
                start.AddMinutes(30),
                "Outside slot boundaries")));

        Assert.Contains("available provider slot", exception.Message);
        Assert.Empty(await fixture.Context.Bookings.ToListAsync());
    }

    [Fact]
    public async Task Create_rejects_a_double_booking_but_allows_adjacent_slots()
    {
        await using var fixture = await Fixture.CreateAsync();
        var sender = fixture.Services.GetRequiredService<ISender>();
        var start = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero);

        await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.ScheduleId,
            start,
            start.AddMinutes(30),
            "First"));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            sender.Send(new CreateBookingCommand(
                fixture.PatientId,
                fixture.ProviderId,
                fixture.ScheduleId,
                start,
                start.AddMinutes(30),
                "Duplicate")));
        Assert.Contains("overlaps", exception.Message);

        await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.ScheduleId,
            start.AddMinutes(30),
            start.AddMinutes(60),
            "Adjacent"));

        Assert.Equal(2, await fixture.Context.Bookings.CountAsync());
    }

    private sealed class Fixture(
        AyoosDbContext context,
        ServiceProvider services,
        Guid patientId,
        Guid providerId,
        Guid scheduleId) : IAsyncDisposable
    {
        public AyoosDbContext Context { get; } = context;
        public ServiceProvider Services { get; } = services;
        public Guid PatientId { get; } = patientId;
        public Guid ProviderId { get; } = providerId;
        public Guid ScheduleId { get; } = scheduleId;

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            await Context.DisposeAsync();
        }

        public static async Task<Fixture> CreateAsync()
        {
            var practice = Practice.Create(
                "Tenant Under Test",
                $"booking-{Guid.NewGuid():N}",
                "America/New_York",
                new Address("100 Main Street", null, "New York", "NY", "10001", "US"),
                "practice@example.com",
                "+1-212-555-0100",
                DateTimeOffset.UtcNow);
            var options = new DbContextOptionsBuilder<AyoosDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            var tenant = new TenantInfo
            {
                Id = practice.Id.ToString("D"),
                Identifier = practice.Slug,
                Name = practice.Name
            };
            var context = MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(
                tenant,
                options);
            var provider = Provider.Create(
                practice.Id,
                "Maya",
                "Patel",
                "MD",
                "Family Medicine",
                "maya@example.com",
                "+1-212-555-0101",
                DateTimeOffset.UtcNow);
            var patient = Patient.Create(
                practice.Id,
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
            var schedule = AvailabilitySchedule.Create(
                provider.Id,
                practice.Id.ToString("D"),
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                30);

            context.Practices.Add(practice);
            context.Providers.Add(provider);
            context.Patients.Add(patient);
            context.AvailabilitySchedules.Add(schedule);
            await context.SaveChangesAsync();

            var services = new ServiceCollection()
                .AddLogging()
                .AddApplication()
                .AddSingleton<IProviderRepository>(new ProviderRepository(context))
                .AddSingleton<IPatientRepository>(new PatientRepository(context))
                .AddSingleton<IBookingRepository>(new BookingRepository(context))
                .BuildServiceProvider();

            return new Fixture(
                context,
                services,
                patient.Id,
                provider.Id,
                schedule.Id);
        }
    }
}
