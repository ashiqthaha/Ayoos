using Ayoos.Domain.PracticeInvitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ayoos.Infrastructure.Persistence.Configurations;

internal sealed class PracticeInvitationConfiguration
    : IEntityTypeConfiguration<PracticeInvitation>
{
    public void Configure(EntityTypeBuilder<PracticeInvitation> builder)
    {
        builder.ToTable("PracticeInvitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique();

        builder.Property(invitation => invitation.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(invitation => invitation.PracticeAdminKeycloakUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(invitation => invitation.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(invitation => invitation.Status);

        builder.Property(invitation => invitation.ExpiresAt)
            .IsRequired();

        builder.Property(invitation => invitation.CreatedByKeycloakUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(invitation => invitation.CreatedAt)
            .IsRequired();
    }
}
