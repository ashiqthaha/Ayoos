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
        builder.Property(exception => exception.ExceptionType)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(exception => exception.StartTime)
            .HasColumnType("time without time zone");
        builder.Property(exception => exception.EndTime)
            .HasColumnType("time without time zone");
        builder.Property(exception => exception.Reason).HasMaxLength(500);
        builder.Property(exception => exception.CreatedAtUtc).IsRequired();
        builder.Property(exception => exception.UpdatedAtUtc);

        builder.HasIndex(exception => new
            { exception.TenantId, exception.ProviderId, exception.Date })
            .IsUnique();
    }
}
