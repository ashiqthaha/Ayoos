using Ayoos.Application.Users;

namespace Ayoos.Application.Common.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<ManagedUserModel>> ListPracticeUsersAsync(
        Guid practiceId,
        CancellationToken cancellationToken);

    Task<ManagedUserModel?> GetUserAsync(
        Guid practiceId,
        string userId,
        CancellationToken cancellationToken);

    Task<CreatedUserModel> CreateUserAsync(
        Guid practiceId,
        string email,
        string? firstName,
        string? lastName,
        string role,
        CancellationToken cancellationToken);

    Task SetUserEnabledAsync(
        Guid practiceId,
        string userId,
        bool enabled,
        CancellationToken cancellationToken);

    Task<string> ResetUserPasswordAsync(
        Guid practiceId,
        string userId,
        CancellationToken cancellationToken);

    Task ChangeUserRoleAsync(
        Guid practiceId,
        string userId,
        string role,
        CancellationToken cancellationToken);
}
