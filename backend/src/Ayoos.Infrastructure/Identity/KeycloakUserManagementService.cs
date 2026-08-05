using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Users;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Configuration;

namespace Ayoos.Infrastructure.Identity;

internal sealed class KeycloakUserManagementService(
    IKeycloakClient keycloakClient,
    KeycloakAdminTokenProvider tokenProvider,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IUserManagementService, IPracticeInvitationUserService
{
    private const string PracticeAttribute = "practice";
    private const string RoleAttribute = "role";
    private const string UpdatePasswordAction = "UPDATE_PASSWORD";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> CreatePracticeAdminAsync(
        string email,
        CancellationToken cancellationToken)
    {
        const string role = "practice-admin";
        var (realm, _) = GetKeycloakConfiguration();
        var temporaryPassword = configuration["Keycloak:InvitationTemporaryPassword"]
            ?? throw new InvalidOperationException(
                "Configuration value 'Keycloak:InvitationTemporaryPassword' was not configured.");
        var user = new UserRepresentation
        {
            Username = email,
            Email = email,
            Enabled = true,
            EmailVerified = false,
            Attributes = new Dictionary<string, ICollection<string>>
            {
                [RoleAttribute] = new List<string> { role }
            },
            RequiredActions = new List<string> { UpdatePasswordAction },
            Credentials = new List<CredentialRepresentation>
            {
                new()
                {
                    Type = "password",
                    Value = temporaryPassword,
                    Temporary = true
                }
            }
        };

        using var client = await CreateAuthenticatedHttpClientAsync(cancellationToken);
        using var response = await client.PostAsync(
            $"admin/realms/{Uri.EscapeDataString(realm)}/users",
            CreateJsonContent(user),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConflictException(
                $"A Keycloak user with email '{email}' already exists.");
        }

        response.EnsureSuccessStatusCode();
        var userId = GetCreatedUserId(response.Headers.Location);

        try
        {
            await AssignRealmRoleAsync(userId, role, cancellationToken);
        }
        catch
        {
            try
            {
                await DeleteUserAsync(userId, cancellationToken);
            }
            catch
            {
                // Preserve the role-assignment failure; deleting the partial user is best effort.
            }

            throw;
        }

        return userId;
    }

    public async Task AssignPracticeAsync(
        string keycloakUserId,
        string practiceSlug,
        CancellationToken cancellationToken)
    {
        var (realm, _) = GetKeycloakConfiguration();
        var userUri =
            $"admin/realms/{Uri.EscapeDataString(realm)}/users/{Uri.EscapeDataString(keycloakUserId)}";
        using var client = await CreateAuthenticatedHttpClientAsync(cancellationToken);
        using var getResponse = await client.GetAsync(userUri, cancellationToken);
        if (getResponse.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(
                $"Keycloak user '{keycloakUserId}' was not found.");
        }

        getResponse.EnsureSuccessStatusCode();
        var user = await DeserializeAsync<UserRepresentation>(
            getResponse,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Keycloak did not return user '{keycloakUserId}'.");
        user.Attributes ??= new Dictionary<string, ICollection<string>>();
        user.Attributes[PracticeAttribute] = new List<string>
        {
            practiceSlug
        };

        using var putResponse = await client.PutAsync(
            userUri,
            CreateJsonContent(user),
            cancellationToken);
        putResponse.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(
        string keycloakUserId,
        CancellationToken cancellationToken)
    {
        var (realm, _) = GetKeycloakConfiguration();
        using var client = await CreateAuthenticatedHttpClientAsync(cancellationToken);
        using var response = await client.DeleteAsync(
            $"admin/realms/{Uri.EscapeDataString(realm)}/users/{Uri.EscapeDataString(keycloakUserId)}",
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyList<ManagedUserModel>> ListPracticeUsersAsync(
        Guid practiceId,
        CancellationToken cancellationToken)
    {
        var (realm, _) = GetKeycloakConfiguration();
        var query = $"{PracticeAttribute}:{PracticeValue(practiceId)}";
        var requestUrl =
            $"admin/realms/{Uri.EscapeDataString(realm)}/users?q={query}&briefRepresentation=false&max=500";

        using var client = await CreateAuthenticatedHttpClientAsync(cancellationToken);
        using var response = await client.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await DeserializeAsync<List<UserRepresentation>>(
            response,
            cancellationToken) ?? [];

        return users.Select(MapUser).ToList();
    }

    public async Task<ManagedUserModel?> GetUserAsync(
        Guid practiceId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!await EnsureUserInPracticeAsync(
                practiceId,
                userId,
                cancellationToken))
        {
            return null;
        }

        var (realm, _) = GetKeycloakConfiguration();
        var user = await GetUserOrNullAsync(realm, userId, cancellationToken);
        return user is null || string.IsNullOrWhiteSpace(user.Id)
            ? null
            : MapUser(user);
    }

    public async Task<CreatedUserModel> CreateUserAsync(
        Guid practiceId,
        string email,
        string? firstName,
        string? lastName,
        string role,
        CancellationToken cancellationToken)
    {
        var (realm, _) = GetKeycloakConfiguration();
        var temporaryPassword = GenerateTemporaryPassword();
        var user = new UserRepresentation
        {
            Username = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Enabled = true,
            EmailVerified = false,
            Attributes = new Dictionary<string, ICollection<string>>
            {
                [PracticeAttribute] = new List<string>
                {
                    PracticeValue(practiceId)
                },
                [RoleAttribute] = new List<string>
                {
                    role
                }
            },
            RequiredActions = new List<string>
            {
                UpdatePasswordAction
            },
            Credentials = new List<CredentialRepresentation>
            {
                new()
                {
                    Type = "password",
                    Value = temporaryPassword,
                    Temporary = true
                }
            }
        };

        using var response = await keycloakClient.CreateUserWithResponseAsync(
            realm,
            user,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var userId = GetCreatedUserId(response.Headers.Location);
        await AssignRealmRoleAsync(userId, role, cancellationToken);

        var createdUser = await keycloakClient.GetUserAsync(
            realm,
            userId,
            false,
            cancellationToken);
        if (createdUser is null || string.IsNullOrWhiteSpace(createdUser.Id))
        {
            throw new InvalidOperationException(
                "Keycloak did not return the newly created user.");
        }

        return new CreatedUserModel(
            MapUser(createdUser),
            temporaryPassword);
    }

    public async Task SetUserEnabledAsync(
        Guid practiceId,
        string userId,
        bool enabled,
        CancellationToken cancellationToken)
    {
        await ThrowIfUserNotInPracticeAsync(
            practiceId,
            userId,
            cancellationToken);

        var (realm, _) = GetKeycloakConfiguration();
        var user = await keycloakClient.GetUserAsync(
            realm,
            userId,
            false,
            cancellationToken);
        user.Enabled = enabled;

        await keycloakClient.UpdateUserAsync(
            realm,
            userId,
            user,
            cancellationToken);
    }

    public async Task<string> ResetUserPasswordAsync(
        Guid practiceId,
        string userId,
        CancellationToken cancellationToken)
    {
        await ThrowIfUserNotInPracticeAsync(
            practiceId,
            userId,
            cancellationToken);

        var (realm, _) = GetKeycloakConfiguration();
        var temporaryPassword = GenerateTemporaryPassword();
        await keycloakClient.ResetPasswordAsync(
            realm,
            userId,
            new CredentialRepresentation
            {
                Type = "password",
                Value = temporaryPassword,
                Temporary = true
            },
            cancellationToken);

        var user = await keycloakClient.GetUserAsync(
            realm,
            userId,
            false,
            cancellationToken);
        user.RequiredActions ??= new List<string>();
        if (!user.RequiredActions.Contains(
                UpdatePasswordAction,
                StringComparer.Ordinal))
        {
            user.RequiredActions.Add(UpdatePasswordAction);
        }

        await keycloakClient.UpdateUserAsync(
            realm,
            userId,
            user,
            cancellationToken);

        return temporaryPassword;
    }

    public async Task ChangeUserRoleAsync(
        Guid practiceId,
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        await ThrowIfUserNotInPracticeAsync(
            practiceId,
            userId,
            cancellationToken);

        var (realm, _) = GetKeycloakConfiguration();
        var escapedRealm = Uri.EscapeDataString(realm);
        var escapedUserId = Uri.EscapeDataString(userId);
        var roleMappingsUrl =
            $"admin/realms/{escapedRealm}/users/{escapedUserId}/role-mappings/realm";

        using (var client = await CreateAuthenticatedHttpClientAsync(cancellationToken))
        {
            using var getResponse = await client.GetAsync(
                roleMappingsUrl,
                cancellationToken);
            getResponse.EnsureSuccessStatusCode();

            var assignedRoles = await DeserializeAsync<List<RoleRepresentation>>(
                getResponse,
                cancellationToken) ?? [];
            var assignableRoles = assignedRoles
                .Where(assignedRole => UserValidation.AssignableRoles.Contains(
                    assignedRole.Name,
                    StringComparer.Ordinal))
                .ToList();

            if (assignableRoles.Count > 0)
            {
                using var deleteRequest = new HttpRequestMessage(
                    HttpMethod.Delete,
                    roleMappingsUrl)
                {
                    Content = CreateJsonContent(assignableRoles)
                };
                using var deleteResponse = await client.SendAsync(
                    deleteRequest,
                    cancellationToken);
                deleteResponse.EnsureSuccessStatusCode();
            }
        }

        await AssignRealmRoleAsync(userId, role, cancellationToken);

        var user = await keycloakClient.GetUserAsync(
            realm,
            userId,
            false,
            cancellationToken);
        user.Attributes ??= new Dictionary<string, ICollection<string>>();
        user.Attributes[RoleAttribute] = new List<string>
        {
            role
        };

        await keycloakClient.UpdateUserAsync(
            realm,
            userId,
            user,
            cancellationToken);
    }

    private async Task AssignRealmRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var (realm, _) = GetKeycloakConfiguration();
        var escapedRealm = Uri.EscapeDataString(realm);
        var roleUrl =
            $"admin/realms/{escapedRealm}/roles/{Uri.EscapeDataString(roleName)}";

        using var client = await CreateAuthenticatedHttpClientAsync(cancellationToken);
        using var roleResponse = await client.GetAsync(
            roleUrl,
            cancellationToken);
        roleResponse.EnsureSuccessStatusCode();

        var realmRole = await DeserializeAsync<RoleRepresentation>(
            roleResponse,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Keycloak did not return realm role '{roleName}'.");
        var roleMappingsUrl =
            $"admin/realms/{escapedRealm}/users/{Uri.EscapeDataString(userId)}/role-mappings/realm";
        using var assignRequest = new HttpRequestMessage(
            HttpMethod.Post,
            roleMappingsUrl)
        {
            Content = CreateJsonContent(new[]
            {
                realmRole
            })
        };
        using var assignResponse = await client.SendAsync(
            assignRequest,
            cancellationToken);
        assignResponse.EnsureSuccessStatusCode();
    }

    private async Task<bool> EnsureUserInPracticeAsync(
        Guid practiceId,
        string userId,
        CancellationToken cancellationToken)
    {
        var (realm, _) = GetKeycloakConfiguration();
        var user = await GetUserOrNullAsync(realm, userId, cancellationToken);

        return user is not null
            && !string.IsNullOrWhiteSpace(user.Id)
            && string.Equals(
                GetFirstAttributeValue(user, PracticeAttribute),
                PracticeValue(practiceId),
                StringComparison.Ordinal);
    }

    private async Task ThrowIfUserNotInPracticeAsync(
        Guid practiceId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!await EnsureUserInPracticeAsync(
                practiceId,
                userId,
                cancellationToken))
        {
            throw new NotFoundException($"User '{userId}' was not found.");
        }
    }

    private async Task<UserRepresentation?> GetUserOrNullAsync(
        string realm,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await keycloakClient.GetUserAsync(
                realm,
                userId,
                false,
                cancellationToken);
        }
        catch (KeycloakHttpClientException exception)
            when (exception.StatusCode == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<HttpClient> CreateAuthenticatedHttpClientAsync(
        CancellationToken cancellationToken)
    {
        var (_, adminBaseUrl) = GetKeycloakConfiguration();
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(
            $"{adminBaseUrl.TrimEnd('/')}/",
            UriKind.Absolute);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await tokenProvider.GetAccessTokenAsync(cancellationToken));

        return client;
    }

    private (string Realm, string AdminBaseUrl) GetKeycloakConfiguration()
    {
        var realm = configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException(
                "Configuration value 'Keycloak:Realm' was not configured.");
        var adminBaseUrl = configuration["Keycloak:AdminBaseUrl"]
            ?? throw new InvalidOperationException(
                "Configuration value 'Keycloak:AdminBaseUrl' was not configured.");

        return (realm, adminBaseUrl);
    }

    private static async Task<T?> DeserializeAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var responseStream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(
            responseStream,
            JsonOptions,
            cancellationToken);
    }

    private static StringContent CreateJsonContent<T>(T value) =>
        new(
            JsonSerializer.Serialize(value, JsonOptions),
            Encoding.UTF8,
            "application/json");

    private static string GetCreatedUserId(Uri? location)
    {
        var path = location switch
        {
            null => null,
            { IsAbsoluteUri: true } => location.AbsolutePath,
            _ => location.OriginalString.Split('?', '#')[0]
        };
        var userId = path?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrWhiteSpace(userId)
            ? throw new InvalidOperationException(
                "Keycloak did not return the created user's location.")
            : Uri.UnescapeDataString(userId);
    }

    private static ManagedUserModel MapUser(UserRepresentation user) =>
        new(
            user.Id!,
            user.Username!,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Enabled ?? false,
            GetFirstAttributeValue(user, RoleAttribute));

    private static string? GetFirstAttributeValue(
        UserRepresentation user,
        string attributeName)
    {
        if (user.Attributes is null
            || !user.Attributes.TryGetValue(attributeName, out var values))
        {
            return null;
        }

        return values.FirstOrDefault();
    }

    private static string PracticeValue(Guid practiceId) =>
        practiceId.ToString("D");

    private static string GenerateTemporaryPassword()
    {
        const int passwordLength = 16;
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*";
        const string allCharacters = uppercase + lowercase + digits + symbols;

        Span<char> password = stackalloc char[passwordLength];
        password[0] = GetRandomCharacter(uppercase);
        password[1] = GetRandomCharacter(lowercase);
        password[2] = GetRandomCharacter(digits);
        password[3] = GetRandomCharacter(symbols);

        for (var index = 4; index < password.Length; index++)
        {
            password[index] = GetRandomCharacter(allCharacters);
        }

        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) =
                (password[swapIndex], password[index]);
        }

        return new string(password);
    }

    private static char GetRandomCharacter(string characters) =>
        characters[RandomNumberGenerator.GetInt32(characters.Length)];
}
