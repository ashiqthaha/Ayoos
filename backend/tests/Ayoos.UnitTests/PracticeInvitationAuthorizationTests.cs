using Ayoos.Api.Authentication;
using Ayoos.Api.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ayoos.UnitTests;

public sealed class PracticeInvitationAuthorizationTests
{
    [Fact]
    public async Task SuperAdmin_policy_requires_distinct_ayoos_superadmin_role()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Authority"] = "https://identity.example/realms/ayoos",
                ["Keycloak:Audience"] = "ayoos-api"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeycloakAuthentication(configuration);
        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync(AuthorizationPolicies.SuperAdmin);
        var roles = Assert.Single(policy!.Requirements.OfType<RolesAuthorizationRequirement>());

        Assert.Equal(new[] { "ayoos-superadmin" }, roles.AllowedRoles);
        Assert.DoesNotContain("practice-admin", roles.AllowedRoles);
    }

    [Fact]
    public void Admin_invitation_routes_require_superadmin_and_setup_check_is_anonymous()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<ISender>(_ => null!);
        using var app = builder.Build();

        app.MapPracticeInvitationEndpoints();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();
        var adminEndpoints = endpoints
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith(
                "/api/admin/invitations",
                StringComparison.Ordinal) == true)
            .ToArray();
        var setupEndpoint = Assert.Single(
            endpoints,
            endpoint =>
                endpoint.RoutePattern.RawText == "/api/setup/invitations/{token}");

        Assert.Equal(3, adminEndpoints.Length);
        Assert.All(adminEndpoints, endpoint =>
            Assert.Contains(
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                metadata => metadata.Policy == AuthorizationPolicies.SuperAdmin));
        Assert.NotNull(setupEndpoint.Metadata.GetMetadata<IAllowAnonymous>());
    }
}
