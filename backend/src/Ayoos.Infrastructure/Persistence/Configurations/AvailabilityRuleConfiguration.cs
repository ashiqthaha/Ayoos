using Ayoos.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilityRuleConfiguration
    : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AvailabilityRules");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.ProviderId).IsRequired();
        builder.Property(rule => rule.DayOfWeek).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.StartTime)
            .HasColumnType("time without time zone")
            .IsRequired();
        builder.Property(rule => rule.EndTime)
            .HasColumnType("time without time zone")
            .IsRequired();
        builder.Property(rule => rule.SlotDurationMinutes)
            .HasDefaultValue(30)
            .IsRequired();
        builder.Property(rule => rule.EffectiveFrom)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(rule => rule.EffectiveTo)
            .HasColumnType("date");

        builder.HasIndex(rule => new { rule.ProviderId, rule.DayOfWeek });
    }
}
