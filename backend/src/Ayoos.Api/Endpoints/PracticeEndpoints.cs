using Ayoos.Application.Practices;
using Ayoos.Application.Practices.CreatePractice;
using Ayoos.Application.Practices.GetPractice;
using Ayoos.Application.Practices.UpdatePractice;
using Ayoos.Api.Authentication;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ayoos.Api.Endpoints;

internal static class PracticeEndpoints
{
    public static IEndpointRouteBuilder MapPracticeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/practices")
            .WithTags("Practices")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost(string.Empty, CreatePracticeAsync)
            .WithName("CreatePractice")
            .WithSummary("Creates a practice and registers it as a tenant.")
            .Produces<PracticeModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.PracticeAdmin);

        group.MapGet("/{slug}", GetPracticeAsync)
            .WithName("GetPractice")
            .WithSummary("Gets the current tenant's practice by slug.")
            .Produces<PracticeModel>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{slug}", UpdatePracticeAsync)
            .WithName("UpdatePractice")
            .WithSummary("Updates the current tenant's practice.")
            .Produces<PracticeModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        return endpoints;
    }

    private static async Task<IResult> CreatePracticeAsync(
        CreatePracticeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePracticeCommand(
                request.Name,
                request.Slug,
                request.TimeZone,
                request.Address?.ToModel()!,
                request.ContactEmail,
                request.ContactPhone,
                request.RawToken),
            cancellationToken);

        return Results.Created($"/api/practices/{result.Slug}", result);
    }

    private static async Task<IResult> GetPracticeAsync(
        string slug,
        [FromHeader(Name = "X-Tenant")] string? tenant,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (httpContext.GetMultiTenantContext<TenantInfo>().TenantInfo is null)
        {
            return TenantNotFound(tenant);
        }

        var result = await sender.Send(new GetPracticeQuery(slug), cancellationToken);

        return result is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Practice not found.")
            : Results.Ok(result);
    }

    private static async Task<IResult> UpdatePracticeAsync(
        string slug,
        [FromHeader(Name = "X-Tenant")] string? tenant,
        UpdatePracticeRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (httpContext.GetMultiTenantContext<TenantInfo>().TenantInfo is null)
        {
            return TenantNotFound(tenant);
        }

        var result = await sender.Send(
            new UpdatePracticeCommand(
                slug,
                request.Name,
                request.Slug,
                request.TimeZone,
                request.Address?.ToModel()!,
                request.ContactEmail,
                request.ContactPhone,
                request.IsActive),
            cancellationToken);

        return Results.Ok(result);
    }

    private static IResult TenantNotFound(string? tenant) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Tenant not found.",
            detail: string.IsNullOrWhiteSpace(tenant)
                ? "A practice or tenant token claim, or the X-Tenant header, is required."
                : $"No tenant registration was found for '{tenant}'.");
}

internal sealed record PracticeAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country)
{
    public PracticeAddressModel ToModel() =>
        new(Line1, Line2, City, State, PostalCode, Country);
}

internal sealed record CreatePracticeRequest(
    string Name,
    string Slug,
    string TimeZone,
    PracticeAddressRequest? Address,
    string ContactEmail,
    string ContactPhone,
    string RawToken);

internal sealed record UpdatePracticeRequest(
    string Name,
    string Slug,
    string TimeZone,
    PracticeAddressRequest? Address,
    string ContactEmail,
    string ContactPhone,
    bool IsActive);
