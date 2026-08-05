using Ayoos.Domain.Bookings;
using Ayoos.Application.Bookings;
using Ayoos.Domain.Patients;
using Ayoos.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", table =>
            table.HasCheckConstraint(
                "CK_Bookings_ScheduledStartBeforeEnd",
                "\"ScheduledStart\" < \"ScheduledEnd\""));
        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.TenantId).IsRequired();
        builder.Property(booking => booking.PatientId).IsRequired();
        builder.Property(booking => booking.ProviderId).IsRequired();
        builder.Property(booking => booking.AvailabilityScheduleId);
        builder.Property(booking => booking.ScheduledStart).IsRequired();
        builder.Property(booking => booking.ScheduledEnd).IsRequired();
        builder.Property(booking => booking.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(booking => booking.Reason)
            .HasMaxLength(BookingValidation.MaximumReasonLength);
        builder.Property(booking => booking.CancellationReason)
            .HasMaxLength(BookingValidation.MaximumCancellationReasonLength);
        builder.Property(booking => booking.CreatedAt).IsRequired();
        builder.Property(booking => booking.UpdatedAt).IsRequired();
        builder.Property(booking => booking.RowVersion).IsRowVersion();

        builder.HasIndex(booking => new
        {
            booking.TenantId,
            booking.ProviderId,
            booking.ScheduledStart
        })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Confirmed')");
        builder.HasIndex(booking => new
        {
            booking.TenantId,
            booking.ProviderId,
            booking.ScheduledStart,
            booking.ScheduledEnd,
            booking.Status
        });
        builder.HasIndex(booking => new
        {
            booking.TenantId,
            booking.PatientId,
            booking.ScheduledStart
        });

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(booking => booking.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(booking => booking.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AvailabilitySchedule>()
            .WithMany()
            .HasForeignKey(booking => booking.AvailabilityScheduleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
