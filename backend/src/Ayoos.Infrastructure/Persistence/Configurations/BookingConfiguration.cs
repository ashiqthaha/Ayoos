using Ayoos.Domain.Bookings;
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
                "CK_Bookings_StartBeforeEnd",
                "\"StartTime\" < \"EndTime\""));
        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.TenantId).IsRequired();
        builder.Property(booking => booking.PatientId).IsRequired();
        builder.Property(booking => booking.ProviderId).IsRequired();
        builder.Property(booking => booking.AvailabilityScheduleId);
        builder.Property(booking => booking.StartTime).IsRequired();
        builder.Property(booking => booking.EndTime).IsRequired();
        builder.Property(booking => booking.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(booking => booking.Reason)
            .HasMaxLength(1000);
        builder.Property(booking => booking.CreatedAt).IsRequired();

        builder.HasIndex(booking => new
        {
            booking.TenantId,
            booking.ProviderId,
            booking.StartTime,
            booking.EndTime,
            booking.Status
        });
        builder.HasIndex(booking => new
        {
            booking.TenantId,
            booking.PatientId,
            booking.StartTime
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
