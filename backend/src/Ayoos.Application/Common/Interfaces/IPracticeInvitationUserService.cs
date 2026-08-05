namespace Ayoos.Application.Common.Interfaces;

public interface IPracticeInvitationUserService
{
    Task<string> CreatePracticeAdminAsync(
        string email,
        CancellationToken cancellationToken);

    Task AssignPracticeAsync(
        string keycloakUserId,
        string practiceSlug,
        CancellationToken cancellationToken);

    Task DeleteUserAsync(
        string keycloakUserId,
        CancellationToken cancellationToken);
}
