using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Ayoos.Infrastructure.Identity;

internal sealed class KeycloakAdminTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
{
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromSeconds(30);
    private readonly Lock _cacheLock = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (TryGetCachedToken(out var accessToken))
        {
            return accessToken;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            if (TryGetCachedToken(out accessToken))
            {
                return accessToken;
            }

            var adminBaseUrl = GetRequiredConfiguration("Keycloak:AdminBaseUrl")
                .TrimEnd('/');
            var realm = GetRequiredConfiguration("Keycloak:Realm");
            var clientId = GetRequiredConfiguration("Keycloak:AdminClientId");
            var clientSecret = GetRequiredConfiguration("Keycloak:AdminClientSecret");
            var tokenUrl = $"{adminBaseUrl}/realms/{Uri.EscapeDataString(realm)}/protocol/openid-connect/token";

            using var client = httpClientFactory.CreateClient();
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });
            using var response = await client.PostAsync(tokenUrl, form, ct);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: ct);

            accessToken = document.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException(
                    "The Keycloak token response did not contain an access token.");
            }

            var expiresIn = document.RootElement.GetProperty("expires_in").GetInt32();
            lock (_cacheLock)
            {
                _accessToken = accessToken;
                _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            }

            return accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool TryGetCachedToken(out string accessToken)
    {
        lock (_cacheLock)
        {
            if (!string.IsNullOrWhiteSpace(_accessToken)
                && DateTimeOffset.UtcNow < _expiresAtUtc - RefreshBuffer)
            {
                accessToken = _accessToken;
                return true;
            }
        }

        accessToken = string.Empty;
        return false;
    }

    private string GetRequiredConfiguration(string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"Configuration value '{key}' was not configured.");
}
