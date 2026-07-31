using Ayoos.Api.Authentication;
using Ayoos.Application.Providers;
using Ayoos.Application.Providers.AddAvailabilityException;
using Ayoos.Application.Providers.CreateProvider;
using Ayoos.Application.Providers.DeactivateProvider;
using Ayoos.Application.Providers.GetProvider;
using Ayoos.Application.Providers.GetProviderAvailability;
using Ayoos.Application.Providers.GetProviderAvailabilityExceptions;
using Ayoos.Application.Providers.GetProviderAvailabilityRules;
using Ayoos.Application.Providers.ListProviders;
using Ayoos.Application.Providers.RemoveAvailabilityException;
using Ayoos.Application.Providers.SetAvailabilityRules;
using Ayoos.Application.Providers.UpdateProvider;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ayoos.Api.Endpoints;

internal static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/providers")
            .WithTags("Providers")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .AddEndpointFilter(async (context, next) =>
            {
                var httpContext = context.HttpContext;
                if (httpContext.GetMultiTenantContext<TenantInfo>().TenantInfo is not null)
                {
                    return await next(context);
                }

                var tenant = httpContext.User.FindFirst("practice")?.Value
                    ?? httpContext.User.FindFirst("tenant")?.Value
                    ?? httpContext.Request.Headers["X-Tenant"].ToString();
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Tenant not found.",
                    detail: string.IsNullOrWhiteSpace(tenant)
                        ? "A practice or tenant token claim, or the X-Tenant header, is required."
                        : $"No tenant registration was found for '{tenant}'.");
            });

        group.MapGet(string.Empty, ListProvidersAsync)
            .WithName("ListProviders")
            .WithSummary("Lists providers in the current practice tenant.")
            .Produces<IReadOnlyList<ProviderModel>>();

        group.MapPost(string.Empty, CreateProviderAsync)
            .WithName("CreateProvider")
            .WithSummary("Creates a provider in the current practice tenant.")
            .Produces<ProviderModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/{id:guid}", GetProviderAsync)
            .WithName("GetProvider")
            .WithSummary("Gets a provider in the current practice tenant.")
            .Produces<ProviderModel>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateProviderAsync)
            .WithName("UpdateProvider")
            .WithSummary("Updates a provider's profile.")
            .Produces<ProviderModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapPost("/{id:guid}/deactivate", DeactivateProviderAsync)
            .WithName("DeactivateProvider")
            .WithSummary("Deactivates a provider.")
            .Produces<ProviderModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/{id:guid}/availability-rules", GetAvailabilityRulesAsync)
            .WithName("GetProviderAvailabilityRules")
            .WithSummary("Gets the provider's weekly availability rules.")
            .Produces<IReadOnlyList<AvailabilityRuleModel>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/availability-rules", SetAvailabilityRulesAsync)
            .WithName("SetProviderAvailabilityRules")
            .WithSummary("Replaces the provider's complete weekly availability rule set.")
            .Produces<IReadOnlyList<AvailabilityRuleModel>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet(
                "/{id:guid}/availability-exceptions",
                GetAvailabilityExceptionsAsync)
            .WithName("GetProviderAvailabilityExceptions")
            .WithSummary("Gets one-off availability exceptions for a provider.")
            .Produces<IReadOnlyList<AvailabilityExceptionModel>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(
                "/{id:guid}/availability-exceptions",
                AddAvailabilityExceptionAsync)
            .WithName("AddProviderAvailabilityException")
            .WithSummary("Adds time off or custom hours for one date.")
            .Produces<AvailabilityExceptionModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapDelete(
                "/{id:guid}/availability-exceptions",
                RemoveAvailabilityExceptionAsync)
            .WithName("RemoveProviderAvailabilityException")
            .WithSummary("Removes an availability exception by exceptionId query parameter.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/{id:guid}/slots", GetSlotsAsync)
            .WithName("GetProviderSlots")
            .WithSummary("Generates open slots in an inclusive date range.")
            .Produces<IReadOnlyList<AvailabilitySlotModel>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> ListProvidersAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ListProvidersQuery(), cancellationToken));

    private static async Task<IResult> CreateProviderAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ProviderRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCreateCommand(), cancellationToken);
        return Results.Created($"/api/providers/{result.Id}", result);
    }

    private static async Task<IResult> GetProviderAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProviderQuery(id), cancellationToken);

        return result is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Provider not found.")
            : Results.Ok(result);
    }

    private static async Task<IResult> UpdateProviderAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ProviderRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(request.ToUpdateCommand(id), cancellationToken));

    private static async Task<IResult> DeactivateProviderAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(new DeactivateProviderCommand(id), cancellationToken));

    private static async Task<IResult> GetAvailabilityRulesAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(
                new GetProviderAvailabilityRulesQuery(id),
                cancellationToken));

    private static async Task<IResult> SetAvailabilityRulesAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        SetAvailabilityRulesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var rules = request.Rules?
            .Select(rule => rule.ToInput())
            .ToArray()!;

        return Results.Ok(
            await sender.Send(
                new SetAvailabilityRulesCommand(id, rules),
                cancellationToken));
    }

    private static async Task<IResult> GetAvailabilityExceptionsAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(
                new GetProviderAvailabilityExceptionsQuery(id),
                cancellationToken));

    private static async Task<IResult> AddAvailabilityExceptionAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityExceptionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddAvailabilityExceptionCommand(
                id,
                request.Date,
                request.IsUnavailable,
                request.OverrideStartTime,
                request.OverrideEndTime,
                request.Reason),
            cancellationToken);

        return Results.Created(
            $"/api/providers/{id}/availability-exceptions?exceptionId={result.Id}",
            result);
    }

    private static async Task<IResult> RemoveAvailabilityExceptionAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery] Guid exceptionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RemoveAvailabilityExceptionCommand(id, exceptionId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetSlotsAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery(Name = "from")] DateOnly fromDate,
        [FromQuery(Name = "to")] DateOnly toDate,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(
                new GetProviderAvailabilityQuery(id, fromDate, toDate),
                cancellationToken));
}

internal sealed record ProviderRequest(
    string FirstName,
    string LastName,
    string Credentials,
    string Specialty,
    string Email,
    string Phone)
{
    public CreateProviderCommand ToCreateCommand() =>
        new(FirstName, LastName, Credentials, Specialty, Email, Phone);

    public UpdateProviderCommand ToUpdateCommand(Guid providerId) =>
        new(providerId, FirstName, LastName, Credentials, Specialty, Email, Phone);
}

internal sealed record AvailabilityRuleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes = 30,
    DateOnly EffectiveFrom = default,
    DateOnly? EffectiveTo = null)
{
    public AvailabilityRuleInput ToInput() =>
        new(
            DayOfWeek,
            StartTime,
            EndTime,
            SlotDurationMinutes,
            EffectiveFrom,
            EffectiveTo);
}

internal sealed record SetAvailabilityRulesRequest(
    IReadOnlyList<AvailabilityRuleRequest>? Rules);

internal sealed record AvailabilityExceptionRequest(
    DateOnly Date,
    bool IsUnavailable,
    TimeOnly? OverrideStartTime,
    TimeOnly? OverrideEndTime,
    string? Reason);
