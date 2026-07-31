using Ayoos.Api.Endpoints;
using Ayoos.Api.ErrorHandling;
using Ayoos.Api.Authentication;
using Ayoos.Application;
using Ayoos.Infrastructure;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Keycloak__Authority was not configured.");
var keycloakFrontendClientId =
    builder.Configuration["Keycloak:FrontendClientId"] ?? "ayoos-frontend";

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Ayoos API",
        Version = "v1",
        Description = "Backend API for tenant-isolated clinic practices."
    });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Keycloak authorization code flow with PKCE.",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(
                    $"{keycloakAuthority.TrimEnd('/')}/protocol/openid-connect/auth"),
                TokenUrl = new Uri(
                    $"{keycloakAuthority.TrimEnd('/')}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    ["openid"] = "Authenticate with OpenID Connect.",
                    ["profile"] = "Read the signed-in user's profile.",
                    ["email"] = "Read the signed-in user's email address."
                }
            }
        }
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("oauth2", document)] =
            ["openid", "profile", "email"]
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyAyoosMigrationsAsync();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.OAuthClientId(keycloakFrontendClientId);
    options.OAuthUsePkce();
    options.OAuthScopes("openid", "profile", "email");
});
app.UseCors("Frontend");
app.UseAuthentication();
app.UseMultiTenant();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "Ayoos API",
    status = "ready"
}));
app.MapHealthChecks("/health");
app.MapPracticeEndpoints();
app.MapProviderEndpoints();

app.Run();
