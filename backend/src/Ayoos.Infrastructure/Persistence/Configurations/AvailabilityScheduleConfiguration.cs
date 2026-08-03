using Ayoos.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilityScheduleConfiguration
    : IEntityTypeConfiguration<AvailabilitySchedule>
{
    public void Configure(EntityTypeBuilder<AvailabilitySchedule> builder)
    {
        builder.ToTable("AvailabilitySchedules");
        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.ProviderId).IsRequired();
        builder.Property(schedule => schedule.TenantId).IsRequired();
        builder.Property(schedule => schedule.DayOfWeek)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(schedule => schedule.StartTime)
            .HasColumnType("time without time zone")
            .IsRequired();
        builder.Property(schedule => schedule.EndTime)
            .HasColumnType("time without time zone")
            .IsRequired();
        builder.Property(schedule => schedule.SlotDurationMinutes)
            .HasDefaultValue(30)
            .IsRequired();
        builder.Property(schedule => schedule.IsActive).IsRequired();

        builder.HasIndex(schedule => new
        {
            schedule.TenantId,
            schedule.ProviderId,
            schedule.DayOfWeek,
            schedule.IsActive
        });
    }
}
