using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.PracticeInvitations.CreatePracticeInvitation;
using Ayoos.Domain.PracticeInvitations;
using Microsoft.Extensions.Logging;

namespace Ayoos.UnitTests;

public sealed class CreatePracticeInvitationCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    private const string CreatedUserId = "created-keycloak-user";

    [Fact]
    public async Task Success_persists_invitation_without_deleting_created_user()
    {
        var repository = new RecordingInvitationRepository();
        var userService = new RecordingInvitationUserService();
        var handler = CreateHandler(repository, userService);

        var result = await handler.Handle(
            new CreatePracticeInvitationCommand("Admin@Example.com"),
            CancellationToken.None);

        Assert.Equal(1, userService.CreateCallCount);
        Assert.Equal("admin@example.com", userService.CreatedEmail);
        Assert.NotNull(repository.AddedInvitation);
        Assert.Equal(result.InvitationId, repository.AddedInvitation.Id);
        Assert.Equal(CreatedUserId, repository.AddedInvitation.PracticeAdminKeycloakUserId);
        Assert.Equal(1, repository.SaveCallCount);
        Assert.Empty(userService.DeletedUserIds);
        Assert.StartsWith("http://localhost:3000/setup/", result.SetupUrl);
    }

    [Fact]
    public async Task Persist_failure_deletes_created_user_once_and_rethrows_original_exception()
    {
        var persistenceException = new InvalidOperationException("persist failed");
        var repository = new RecordingInvitationRepository
        {
            SaveException = persistenceException
        };
        var userService = new RecordingInvitationUserService();
        var handler = CreateHandler(repository, userService);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreatePracticeInvitationCommand("admin@example.com"),
                CancellationToken.None));

        Assert.Same(persistenceException, thrown);
        Assert.Equal(new[] { CreatedUserId }, userService.DeletedUserIds);
    }

    [Fact]
    public async Task Compensation_failure_logs_orphan_id_and_rethrows_persistence_exception()
    {
        var persistenceException = new InvalidOperationException("persist failed");
        var compensationException = new HttpRequestException("delete failed");
        var repository = new RecordingInvitationRepository
        {
            SaveException = persistenceException
        };
        var userService = new RecordingInvitationUserService
        {
            DeleteException = compensationException
        };
        var logger = new RecordingLogger<CreatePracticeInvitationCommandHandler>();
        var handler = CreateHandler(repository, userService, logger);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new CreatePracticeInvitationCommand("admin@example.com"),
                CancellationToken.None));

        Assert.Same(persistenceException, thrown);
        Assert.Equal(new[] { CreatedUserId }, userService.DeletedUserIds);
        var log = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Same(compensationException, log.Exception);
        Assert.Contains(CreatedUserId, log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preexisting_user_conflict_never_deletes_a_user()
    {
        var conflictException = new ConflictException("User already exists.");
        var repository = new RecordingInvitationRepository();
        var userService = new RecordingInvitationUserService
        {
            CreateException = conflictException
        };
        var handler = CreateHandler(repository, userService);

        var thrown = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(
                new CreatePracticeInvitationCommand("existing@example.com"),
                CancellationToken.None));

        Assert.Same(conflictException, thrown);
        Assert.Empty(userService.DeletedUserIds);
        Assert.Null(repository.AddedInvitation);
        Assert.Equal(0, repository.SaveCallCount);
    }

    private static CreatePracticeInvitationCommandHandler CreateHandler(
        IPracticeInvitationRepository repository,
        IPracticeInvitationUserService userService,
        ILogger<CreatePracticeInvitationCommandHandler>? logger = null) =>
        new(
            repository,
            userService,
            new StubCurrentUserContext(),
            new StubFrontendUrlProvider(),
            new FixedTimeProvider(Now),
            logger ?? new RecordingLogger<CreatePracticeInvitationCommandHandler>());

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class StubCurrentUserContext : ICurrentUserContext
    {
        public string? KeycloakSubject => "superadmin-keycloak-user";
    }

    private sealed class StubFrontendUrlProvider : IFrontendUrlProvider
    {
        public string BuildPracticeSetupUrl(string rawToken) =>
            $"http://localhost:3000/setup/{rawToken}";
    }

    private sealed class RecordingInvitationUserService : IPracticeInvitationUserService
    {
        public int CreateCallCount { get; private set; }
        public string? CreatedEmail { get; private set; }
        public Exception? CreateException { get; init; }
        public Exception? DeleteException { get; init; }
        public List<string> DeletedUserIds { get; } = [];

        public Task<string> CreatePracticeAdminAsync(
            string email,
            CancellationToken cancellationToken)
        {
            CreateCallCount++;
            CreatedEmail = email;

            return CreateException is null
                ? Task.FromResult(CreatedUserId)
                : Task.FromException<string>(CreateException);
        }

        public Task AssignPracticeAsync(
            string keycloakUserId,
            string practiceSlug,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeleteUserAsync(
            string keycloakUserId,
            CancellationToken cancellationToken)
        {
            DeletedUserIds.Add(keycloakUserId);

            return DeleteException is null
                ? Task.CompletedTask
                : Task.FromException(DeleteException);
        }
    }

    private sealed class RecordingInvitationRepository : IPracticeInvitationRepository
    {
        public PracticeInvitation? AddedInvitation { get; private set; }
        public int SaveCallCount { get; private set; }
        public Exception? SaveException { get; init; }

        public Task AddAsync(
            PracticeInvitation invitation,
            CancellationToken cancellationToken = default)
        {
            AddedInvitation = invitation;
            return Task.CompletedTask;
        }

        public Task<PracticeInvitation?> GetByIdAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PracticeInvitation?>(null);

        public Task<PracticeInvitation?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PracticeInvitation?>(null);

        public Task<(IReadOnlyList<PracticeInvitation> Items, int TotalCount)> ListAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<PracticeInvitation>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallCount++;

            return SaveException is null
                ? Task.CompletedTask
                : Task.FromException(SaveException);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        Exception? Exception);
}
