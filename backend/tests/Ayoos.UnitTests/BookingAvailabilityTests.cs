using Ayoos.Application;
using Ayoos.Application.Bookings.CreateBooking;
using Ayoos.Application.Bookings.ConfirmBooking;
using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Bookings.GetProviderSchedule;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Ayoos.Infrastructure.Persistence;
using Ayoos.Infrastructure.Repositories;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ayoos.UnitTests;

public sealed class BookingAvailabilityTests
{
    [Fact]
    public async Task Create_validator_rejects_a_time_that_is_not_a_generated_slot()
    {
        await using var fixture = await Fixture.CreateAsync();
        var sender = fixture.Services.GetRequiredService<ISender>();
        var start = fixture.Start.AddMinutes(5);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CreateBookingCommand(
                fixture.PatientId,
                fixture.ProviderId,
                fixture.ScheduleId,
                start,
                start.AddMinutes(30),
                "Outside slot boundaries")));

        Assert.Contains(exception.Errors, error =>
            error.PropertyName == "ScheduledStart" &&
            error.ErrorMessage.Contains("generated, non-excepted", StringComparison.Ordinal));
        Assert.Empty(await fixture.Context.Bookings.ToListAsync());
    }

    [Fact]
    public async Task Create_validator_rejects_a_slot_removed_by_an_availability_exception()
    {
        await using var fixture = await Fixture.CreateAsync(unavailable: true);
        var sender = fixture.Services.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new CreateBookingCommand(
                fixture.PatientId,
                fixture.ProviderId,
                fixture.ScheduleId,
                fixture.Start,
                fixture.Start.AddMinutes(30),
                "Exception date")));

        Assert.Contains(exception.Errors, error =>
            error.PropertyName == "ScheduledStart" &&
            error.ErrorMessage.Contains("non-excepted", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Overlap_returns_a_preview_and_force_explicitly_confirms_it()
    {
        await using var fixture = await Fixture.CreateAsync(includeOverlappingSchedule: true);
        var sender = fixture.Services.GetRequiredService<ISender>();

        var first = await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.ScheduleId,
            fixture.Start,
            fixture.Start.AddMinutes(30),
            "First"));
        Assert.NotNull(first.Booking);
        Assert.False(first.ConflictPreview.HasConflicts);

        var overlapStart = fixture.Start.AddMinutes(15);
        var preview = await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.OverlappingScheduleId,
            overlapStart,
            overlapStart.AddMinutes(30),
            "Overlapping"));

        Assert.Null(preview.Booking);
        Assert.True(preview.ConflictPreview.HasConflicts);
        Assert.Equal(first.Booking!.Id, Assert.Single(preview.ConflictPreview.Conflicts).Id);

        var confirmed = await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.OverlappingScheduleId,
            overlapStart,
            overlapStart.AddMinutes(30),
            "Overlapping",
            Force: true));

        Assert.NotNull(confirmed.Booking);
        Assert.True(confirmed.ConflictPreview.HasConflicts);
        Assert.Equal(2, await fixture.Context.Bookings.CountAsync());
    }

    [Fact]
    public async Task Provider_schedule_combines_bookings_with_remaining_open_slots()
    {
        await using var fixture = await Fixture.CreateAsync();
        var sender = fixture.Services.GetRequiredService<ISender>();
        await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.ScheduleId,
            fixture.Start,
            fixture.Start.AddMinutes(30),
            "Booked"));

        var date = DateOnly.FromDateTime(fixture.Start.UtcDateTime);
        var schedule = await sender.Send(
            new GetProviderScheduleQuery(fixture.ProviderId, date, date));

        Assert.Single(schedule.Bookings);
        Assert.DoesNotContain(schedule.OpenSlots, slot => slot.StartTime == new TimeOnly(9, 0));
        Assert.Contains(schedule.OpenSlots, slot => slot.StartTime == new TimeOnly(9, 30));
    }

    [Fact]
    public async Task Confirm_rechecks_active_overlaps()
    {
        await using var fixture = await Fixture.CreateAsync(includeOverlappingSchedule: true);
        var sender = fixture.Services.GetRequiredService<ISender>();
        await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.ScheduleId,
            fixture.Start,
            fixture.Start.AddMinutes(30),
            "First"));
        var overlapStart = fixture.Start.AddMinutes(15);
        var forced = await sender.Send(new CreateBookingCommand(
            fixture.PatientId,
            fixture.ProviderId,
            fixture.OverlappingScheduleId,
            overlapStart,
            overlapStart.AddMinutes(30),
            "Second",
            Force: true));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            sender.Send(new ConfirmBookingCommand(forced.Booking!.Id)));

        Assert.Contains("another active booking", exception.Message);
    }

    private sealed class Fixture(
        AyoosDbContext context,
        ServiceProvider services,
        Guid patientId,
        Guid providerId,
        Guid scheduleId,
        Guid? overlappingScheduleId,
        DateTimeOffset start) : IAsyncDisposable
    {
        public AyoosDbContext Context { get; } = context;
        public ServiceProvider Services { get; } = services;
        public Guid PatientId { get; } = patientId;
        public Guid ProviderId { get; } = providerId;
        public Guid ScheduleId { get; } = scheduleId;
        public Guid? OverlappingScheduleId { get; } = overlappingScheduleId;
        public DateTimeOffset Start { get; } = start;

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            await Context.DisposeAsync();
        }

        public static async Task<Fixture> CreateAsync(
            bool unavailable = false,
            bool includeOverlappingSchedule = false)
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
            var tenantId = practice.Id.ToString("D");
            var tenant = new TenantInfo
            {
                Id = tenantId,
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
                tenantId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(11, 0),
                30);
            AvailabilitySchedule? overlappingSchedule = null;
            if (includeOverlappingSchedule)
            {
                overlappingSchedule = AvailabilitySchedule.Create(
                    provider.Id,
                    tenantId,
                    DayOfWeek.Monday,
                    new TimeOnly(9, 15),
                    new TimeOnly(10, 15),
                    30);
            }

            context.Practices.Add(practice);
            context.Providers.Add(provider);
            context.Patients.Add(patient);
            context.AvailabilitySchedules.Add(schedule);
            if (overlappingSchedule is not null)
            {
                context.AvailabilitySchedules.Add(overlappingSchedule);
            }

            if (unavailable)
            {
                context.AvailabilityExceptions.Add(AvailabilityException.Create(
                    provider.Id,
                    tenantId,
                    new DateOnly(2030, 1, 7),
                    AvailabilityExceptionType.Unavailable,
                    null,
                    null,
                    "Closed"));
            }

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
                schedule.Id,
                overlappingSchedule?.Id,
                new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.Zero));
        }
    }
}
