using Ayoos.Domain.Practices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class PracticeConfiguration : IEntityTypeConfiguration<Practice>
{
    public void Configure(EntityTypeBuilder<Practice> builder)
    {
        builder.ToTable("Practices");

        builder.HasKey(practice => practice.Id);

        builder.Property(practice => practice.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(practice => practice.Slug)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(practice => practice.Slug)
            .IsUnique();

        builder.Property(practice => practice.TimeZone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(practice => practice.ContactEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(practice => practice.ContactPhone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(practice => practice.CreatedAtUtc)
            .IsRequired();

        builder.Property(practice => practice.IsActive)
            .IsRequired();

        builder.OwnsOne(practice => practice.Address, address =>
        {
            address.Property(value => value.Line1)
                .HasColumnName("Address_Line1")
                .HasMaxLength(200)
                .IsRequired();
            address.Property(value => value.Line2)
                .HasColumnName("Address_Line2")
                .HasMaxLength(200);
            address.Property(value => value.City)
                .HasColumnName("Address_City")
                .HasMaxLength(100)
                .IsRequired();
            address.Property(value => value.State)
                .HasColumnName("Address_State")
                .HasMaxLength(100)
                .IsRequired();
            address.Property(value => value.PostalCode)
                .HasColumnName("Address_PostalCode")
                .HasMaxLength(20)
                .IsRequired();
            address.Property(value => value.Country)
                .HasColumnName("Address_Country")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(practice => practice.Address).IsRequired();
    }
}
