using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Security;
using Ayoos.Domain.PracticeInvitations;

namespace Ayoos.UnitTests;

public sealed class PracticeInvitationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Token_is_256_bit_base64url_and_only_its_sha256_hash_is_stored()
    {
        var rawToken = PracticeInvitationToken.Generate();
        var decoded = Convert.FromBase64String(
            rawToken
                .Replace('-', '+')
                .Replace('_', '/')
                .PadRight((rawToken.Length + 3) / 4 * 4, '='));
        var invitation = CreateInvitation(rawToken);

        Assert.Equal(PracticeInvitationToken.ByteLength, decoded.Length);
        Assert.Equal(PracticeInvitationToken.Hash(rawToken), invitation.TokenHash);
        Assert.DoesNotContain(rawToken, invitation.TokenHash, StringComparison.Ordinal);
        Assert.DoesNotContain(
            typeof(PracticeInvitation).GetProperties(),
            property => property.Name.Contains("RawToken", StringComparison.Ordinal));
    }

    [Fact]
    public void IsUsable_is_true_only_for_unexpired_pending_invitation()
    {
        var pending = CreateInvitation("pending");
        var consumed = CreateInvitation("consumed");
        consumed.Consume(Guid.NewGuid(), Now);
        var revoked = CreateInvitation("revoked");
        revoked.Revoke();
        var expired = PracticeInvitation.Create(
            PracticeInvitationToken.Hash("expired"),
            "admin@example.com",
            "keycloak-user",
            Now.AddMinutes(-1),
            "superadmin-user",
            Now.AddDays(-1));

        Assert.True(pending.IsUsable(Now));
        Assert.False(pending.IsUsable(Now.AddDays(8)));
        Assert.False(consumed.IsUsable(Now));
        Assert.False(revoked.IsUsable(Now));
        Assert.False(expired.IsUsable(Now));
    }

    [Fact]
    public void Second_domain_consume_and_zero_row_atomic_consume_both_fail()
    {
        var invitation = CreateInvitation("single-use");
        invitation.Consume(Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(
            () => invitation.Consume(Guid.NewGuid(), Now.AddSeconds(1)));
        Assert.Throws<GoneException>(
            () => PracticeInvitationConsumption.EnsureSucceeded(0));
        PracticeInvitationConsumption.EnsureSucceeded(1);
    }

    private static PracticeInvitation CreateInvitation(string rawToken) =>
        PracticeInvitation.Create(
            PracticeInvitationToken.Hash(rawToken),
            "admin@example.com",
            "keycloak-user",
            Now.AddDays(7),
            "superadmin-user",
            Now);
}
