using Ayoos.Domain.Patients;
using Ayoos.Domain.Practices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.PracticeId).IsRequired();
        builder.Property(patient => patient.KeycloakUserId).HasMaxLength(200);
        builder.Property(patient => patient.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(patient => patient.LastName).HasMaxLength(100).IsRequired();
        builder.Property(patient => patient.PreferredName).HasMaxLength(100);
        builder.Property(patient => patient.DateOfBirth).HasColumnType("date").IsRequired();
        builder.Property(patient => patient.Sex).HasConversion<int>().IsRequired();
        builder.Property(patient => patient.Email).HasMaxLength(320).IsRequired();
        builder.Property(patient => patient.Phone).HasMaxLength(50).IsRequired();
        builder.Property(patient => patient.PreferredLanguage).HasMaxLength(100);
        builder.Property(patient => patient.IsActive).IsRequired();
        builder.Property(patient => patient.CreatedAtUtc).IsRequired();
        builder.Property(patient => patient.UpdatedAtUtc);

        builder.OwnsOne(patient => patient.Address, address =>
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
        builder.Navigation(patient => patient.Address).IsRequired();

        builder.HasIndex(patient => new
        {
            patient.PracticeId,
            patient.LastName,
            patient.DateOfBirth
        });
        builder.HasIndex(patient => patient.KeycloakUserId)
            .IsUnique()
            .HasFilter("\"KeycloakUserId\" IS NOT NULL");

        builder.HasOne<Practice>()
            .WithMany()
            .HasForeignKey(patient => patient.PracticeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(patient => patient.EmergencyContact)
            .WithOne()
            .HasForeignKey<EmergencyContact>(contact => contact.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
