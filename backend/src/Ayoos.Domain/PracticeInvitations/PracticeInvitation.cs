using Ayoos.Domain.Common;

namespace Ayoos.Domain.PracticeInvitations;

public sealed class PracticeInvitation : Entity
{
    private PracticeInvitation()
        : base(Guid.NewGuid())
    {
    }

    private PracticeInvitation(
        Guid id,
        string tokenHash,
        string email,
        string practiceAdminKeycloakUserId,
        DateTimeOffset expiresAt,
        string createdByKeycloakUserId,
        DateTimeOffset createdAt)
        : base(id)
    {
        TokenHash = Required(tokenHash, nameof(tokenHash));
        Email = Required(email, nameof(email));
        PracticeAdminKeycloakUserId = Required(
            practiceAdminKeycloakUserId,
            nameof(practiceAdminKeycloakUserId));
        Status = PracticeInvitationStatus.Pending;
        ExpiresAt = expiresAt.ToUniversalTime();
        CreatedByKeycloakUserId = Required(
            createdByKeycloakUserId,
            nameof(createdByKeycloakUserId));
        CreatedAt = createdAt.ToUniversalTime();
    }

    public string TokenHash { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PracticeAdminKeycloakUserId { get; private set; } = string.Empty;

    public PracticeInvitationStatus Status { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public string CreatedByKeycloakUserId { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? PracticeId { get; private set; }

    public static PracticeInvitation Create(
        string tokenHash,
        string email,
        string practiceAdminKeycloakUserId,
        DateTimeOffset expiresAt,
        string createdByKeycloakUserId,
        DateTimeOffset createdAt)
    {
        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "An invitation must expire after it is created.");
        }

        return new PracticeInvitation(
            Guid.NewGuid(),
            tokenHash,
            email.Trim().ToLowerInvariant(),
            practiceAdminKeycloakUserId,
            expiresAt,
            createdByKeycloakUserId,
            createdAt);
    }

    public bool IsUsable(DateTimeOffset now) =>
        Status == PracticeInvitationStatus.Pending
        && ExpiresAt > now.ToUniversalTime();

    public void Consume(Guid practiceId, DateTimeOffset? consumedAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(practiceId, Guid.Empty);

        if (Status != PracticeInvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending invitation can be consumed.");
        }

        Status = PracticeInvitationStatus.Consumed;
        PracticeId = practiceId;
        ConsumedAt = (consumedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }

    public void Revoke()
    {
        if (Status != PracticeInvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending invitation can be revoked.");
        }

        Status = PracticeInvitationStatus.Revoked;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status == PracticeInvitationStatus.Pending
            && ExpiresAt <= now.ToUniversalTime())
        {
            Status = PracticeInvitationStatus.Expired;
        }
    }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
