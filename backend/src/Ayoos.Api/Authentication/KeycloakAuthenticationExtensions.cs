using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Ayoos.Api.Authentication;

internal static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException(
                "Keycloak__Authority was not configured.");
        var audience = configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException(
                "Keycloak__Audience was not configured.");
        var metadataAddress = configuration["Keycloak:MetadataAddress"];

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = authority.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase);
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = ClaimTypes.Role,
                    ValidIssuer = authority
                };

                if (!string.IsNullOrWhiteSpace(metadataAddress))
                {
                    options.MetadataAddress = metadataAddress;
                }

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        AddRealmRoles(context.Principal);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.SuperAdmin,
                policy => policy.RequireRole("ayoos-superadmin"))
            .AddPolicy(
                AuthorizationPolicies.PracticeAdmin,
                policy => policy.RequireRole("practice-admin"))
            .AddPolicy(
                AuthorizationPolicies.ProviderOnly,
                policy => policy.RequireRole("provider"))
            .AddPolicy(
                AuthorizationPolicies.ProviderOrAdmin,
                policy => policy.RequireRole("provider", "practice-admin"))
            .AddPolicy(
                AuthorizationPolicies.PatientOnly,
                policy => policy.RequireRole("patient"))
            .AddPolicy(
                AuthorizationPolicies.StaffOrAdmin,
                policy => policy.RequireRole("staff", "practice-admin"))
            .AddPolicy(
                AuthorizationPolicies.AuthenticatedUser,
                policy => policy.RequireAuthenticatedUser());

        return services;
    }

    private static void AddRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccess);
            if (!document.RootElement.TryGetProperty("roles", out var roles)
                || roles.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var existingRoles = principal.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var role in roles.EnumerateArray())
            {
                var roleName = role.GetString();
                if (!string.IsNullOrWhiteSpace(roleName)
                    && existingRoles.Add(roleName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }
        catch (JsonException)
        {
            // A malformed realm_access claim has no authorization value.
        }
    }
}
