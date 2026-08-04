namespace Ayoos.Application.Users;

public sealed record ManagedUserModel(
    string UserId,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    string? Role);

public sealed record CreatedUserModel(
    ManagedUserModel User,
    string TemporaryPassword);

public static class UserValidation
{
    public const int MaximumNameLength = 100;
    public const int MinimumPasswordLength = 12;

    public static readonly string[] AssignableRoles =
    {
        "practice-admin",
        "provider",
        "staff"
    };
}
