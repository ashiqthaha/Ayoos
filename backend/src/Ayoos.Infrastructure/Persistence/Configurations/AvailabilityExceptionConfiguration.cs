using Ayoos.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class AvailabilityExceptionConfiguration
    : IEntityTypeConfiguration<AvailabilityException>
{
    public void Configure(EntityTypeBuilder<AvailabilityException> builder)
    {
        builder.ToTable("AvailabilityExceptions");
        builder.HasKey(exception => exception.Id);

        builder.Property(exception => exception.ProviderId).IsRequired();
        builder.Property(exception => exception.TenantId).IsRequired();
        builder.Property(exception => exception.Date)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(exception => exception.IsUnavailable).IsRequired();
        builder.Property(exception => exception.OverrideStartTime)
            .HasColumnType("time without time zone");
        builder.Property(exception => exception.OverrideEndTime)
            .HasColumnType("time without time zone");
        builder.Property(exception => exception.Reason).HasMaxLength(500);

        builder.HasIndex(exception => new
            { exception.TenantId, exception.ProviderId, exception.Date })
            .IsUnique();
    }
}
