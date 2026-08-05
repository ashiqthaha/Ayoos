using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Common.Security;
using Ayoos.Application.PracticeInvitations;
using Ayoos.Application.PracticeInvitations.GetInvitationByToken;
using Ayoos.Application.Practices;
using Ayoos.Application.Practices.CreatePractice;
using Ayoos.Domain.PracticeInvitations;
using Ayoos.Domain.Practices;

namespace Ayoos.UnitTests;

public sealed class PracticeInvitationHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePractice_rejects_authenticated_subject_mismatch()
    {
        var repository = new StubInvitationRepository(CreateInvitation());
        var provisioner = new StubPracticeProvisioner();
        var handler = CreateHandler(
            repository,
            provisioner,
            "different-keycloak-user");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));
        Assert.Equal(0, provisioner.CallCount);
    }

    [Theory]
    [InlineData(PracticeInvitationStatus.Consumed)]
    [InlineData(PracticeInvitationStatus.Revoked)]
    [InlineData(PracticeInvitationStatus.Expired)]
    public async Task CreatePractice_rejects_nonusable_invitation(
        PracticeInvitationStatus status)
    {
        var invitation = status == PracticeInvitationStatus.Expired
            ? CreateExpiredInvitation()
            : CreateInvitation();
        if (status == PracticeInvitationStatus.Consumed)
        {
            invitation.Consume(Guid.NewGuid(), Now);
        }
        else if (status == PracticeInvitationStatus.Revoked)
        {
            invitation.Revoke();
        }

        var provisioner = new StubPracticeProvisioner();
        var handler = CreateHandler(
            new StubInvitationRepository(invitation),
            provisioner,
            "invited-keycloak-user");

        await Assert.ThrowsAsync<GoneException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));
        Assert.Equal(0, provisioner.CallCount);
    }

    [Fact]
    public async Task Token_lookup_returns_only_minimal_setup_fields()
    {
        var invitation = CreateInvitation();
        var handler = new GetInvitationByTokenQueryHandler(
            new StubInvitationRepository(invitation),
            new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new GetInvitationByTokenQuery("raw-token"),
            CancellationToken.None);
        var propertyNames = typeof(PracticeInvitationSetupModel)
            .GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray();

        Assert.Equal(invitation.Email, result.Email);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(new[] { "Email", "Status" }, propertyNames);
    }

    private static CreatePracticeCommandHandler CreateHandler(
        IPracticeInvitationRepository repository,
        IPracticeProvisioner provisioner,
        string subject) =>
        new(
            provisioner,
            new StubTenantRegistry(),
            repository,
            new StubCurrentUserContext(subject),
            new FixedTimeProvider(Now));

    private static CreatePracticeCommand ValidCommand() =>
        new(
            "Invited Practice",
            "invited-practice",
            "America/New_York",
            new PracticeAddressModel(
                "100 Main Street",
                null,
                "New York",
                "NY",
                "10001",
                "US"),
            "admin@example.com",
            "+1-212-555-0100",
            "raw-token");

    private static PracticeInvitation CreateInvitation() =>
        PracticeInvitation.Create(
            PracticeInvitationToken.Hash("raw-token"),
            "admin@example.com",
            "invited-keycloak-user",
            Now.AddDays(7),
            "superadmin-user",
            Now);

    private static PracticeInvitation CreateExpiredInvitation() =>
        PracticeInvitation.Create(
            PracticeInvitationToken.Hash("raw-token"),
            "admin@example.com",
            "invited-keycloak-user",
            Now.AddMinutes(-1),
            "superadmin-user",
            Now.AddDays(-1));

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class StubCurrentUserContext(string subject) : ICurrentUserContext
    {
        public string? KeycloakSubject => subject;
    }

    private sealed class StubTenantRegistry : ITenantRegistry
    {
        public Task<bool> IdentifierExistsAsync(
            string identifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task UpdateAsync(
            Guid practiceId,
            string identifier,
            string name,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubPracticeProvisioner : IPracticeProvisioner
    {
        public int CallCount { get; private set; }

        public Task ProvisionAsync(
            Practice practice,
            Guid invitationId,
            string practiceAdminKeycloakUserId,
            DateTimeOffset consumedAt,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubInvitationUserService : IPracticeInvitationUserService
    {
        public Task<string> CreatePracticeAdminAsync(
            string email,
            CancellationToken cancellationToken) =>
            Task.FromResult("invited-keycloak-user");

        public Task AssignPracticeAsync(
            string keycloakUserId,
            string practiceSlug,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteUserAsync(
            string keycloakUserId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class StubInvitationRepository(PracticeInvitation invitation)
        : IPracticeInvitationRepository
    {
        public Task AddAsync(
            PracticeInvitation value,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<PracticeInvitation?> GetByIdAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PracticeInvitation?>(invitation);

        public Task<PracticeInvitation?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PracticeInvitation?>(
                string.Equals(tokenHash, invitation.TokenHash, StringComparison.Ordinal)
                    ? invitation
                    : null);

        public Task<(IReadOnlyList<PracticeInvitation> Items, int TotalCount)> ListAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PracticeInvitation>, int)>(
                (new[] { invitation }, 1));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
