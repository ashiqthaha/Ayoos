using Ayoos.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class EmergencyContactConfiguration
    : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("EmergencyContacts");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.PatientId).IsRequired();
        builder.Property(contact => contact.Name).HasMaxLength(200).IsRequired();
        builder.Property(contact => contact.Relationship).HasMaxLength(100).IsRequired();
        builder.Property(contact => contact.Phone).HasMaxLength(50).IsRequired();
        builder.HasIndex(contact => contact.PatientId).IsUnique();
    }
}
