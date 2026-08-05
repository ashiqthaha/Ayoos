namespace Ayoos.Api.Authentication;

internal static class AuthorizationPolicies
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string PracticeAdmin = nameof(PracticeAdmin);
    public const string ProviderOnly = nameof(ProviderOnly);
    public const string ProviderOrAdmin = nameof(ProviderOrAdmin);
    public const string PatientOnly = nameof(PatientOnly);
    public const string StaffOrAdmin = nameof(StaffOrAdmin);
    public const string AuthenticatedUser = nameof(AuthenticatedUser);
}
