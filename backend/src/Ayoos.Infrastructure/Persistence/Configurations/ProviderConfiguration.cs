using Ayoos.Domain.Practices;
using Ayoos.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers");
        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.PracticeId).IsRequired();
        builder.Property(provider => provider.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(provider => provider.LastName).HasMaxLength(100).IsRequired();
        builder.Property(provider => provider.Credentials).HasMaxLength(50).IsRequired();
        builder.Property(provider => provider.Specialty).HasMaxLength(150).IsRequired();
        builder.Property(provider => provider.Email).HasMaxLength(320).IsRequired();
        builder.Property(provider => provider.Phone).HasMaxLength(50).IsRequired();
        builder.Property(provider => provider.IsActive).IsRequired();
        builder.Property(provider => provider.CreatedAtUtc).IsRequired();

        builder.HasIndex(provider => provider.PracticeId);
        builder.HasIndex(provider => new { provider.LastName, provider.FirstName });

        builder.HasOne<Practice>()
            .WithMany()
            .HasForeignKey(provider => provider.PracticeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(provider => provider.AvailabilitySchedules)
            .WithOne()
            .HasForeignKey(schedule => schedule.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(provider => provider.AvailabilityExceptions)
            .WithOne()
            .HasForeignKey(exception => exception.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(provider => provider.AvailabilitySchedules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(provider => provider.AvailabilityExceptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
