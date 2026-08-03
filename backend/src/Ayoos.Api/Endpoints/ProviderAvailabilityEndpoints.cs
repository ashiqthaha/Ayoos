using Ayoos.Api.Authentication;
using Ayoos.Application.Providers;
using Ayoos.Application.Providers.AddAvailabilityException;
using Ayoos.Application.Providers.CreateAvailability;
using Ayoos.Application.Providers.DeactivateAvailability;
using Ayoos.Application.Providers.GetAvailableSlots;
using Ayoos.Application.Providers.GetProviderAvailability;
using Ayoos.Application.Providers.RemoveAvailabilityException;
using Ayoos.Application.Providers.UpdateAvailability;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ayoos.Api.Endpoints;

internal static class ProviderAvailabilityEndpoints
{
    public static IEndpointRouteBuilder MapProviderAvailabilityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/providers/{providerId:guid}/availability")
            .WithTags("Provider Availability")
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

        group.MapGet(string.Empty, GetAvailabilityAsync)
            .WithName("GetProviderAvailability")
            .WithSummary("Gets active weekly schedules and date-specific exceptions.")
            .Produces<ProviderAvailabilityModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapPost(string.Empty, CreateAvailabilityAsync)
            .WithName("CreateProviderAvailability")
            .WithSummary("Creates a weekly availability schedule.")
            .Produces<AvailabilityScheduleModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapPut("/{availabilityId:guid}", UpdateAvailabilityAsync)
            .WithName("UpdateProviderAvailability")
            .WithSummary("Updates a weekly availability schedule.")
            .Produces<AvailabilityScheduleModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapDelete("/{availabilityId:guid}", DeactivateAvailabilityAsync)
            .WithName("DeactivateProviderAvailability")
            .WithSummary("Deactivates a weekly availability schedule.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapPost("/exceptions", AddAvailabilityExceptionAsync)
            .WithName("AddProviderAvailabilityException")
            .WithSummary("Blocks a date or assigns custom hours for it.")
            .Produces<AvailabilityExceptionModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapDelete("/exceptions/{exceptionId:guid}", RemoveAvailabilityExceptionAsync)
            .WithName("RemoveProviderAvailabilityException")
            .WithSummary("Removes a date-specific availability exception.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapGet("/slots", GetAvailableSlotsAsync)
            .WithName("GetAvailableProviderSlots")
            .WithSummary("Computes concrete bookable slots for an inclusive date range.")
            .Produces<IReadOnlyList<AvailabilitySlotModel>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetAvailabilityAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new GetProviderAvailabilityQuery(providerId),
            cancellationToken));

    private static async Task<IResult> CreateAvailabilityAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityScheduleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            request.ToCreateCommand(providerId),
            cancellationToken);
        return Results.Created(
            $"/api/providers/{providerId}/availability/{result.Id}",
            result);
    }

    private static async Task<IResult> UpdateAvailabilityAsync(
        Guid providerId,
        Guid availabilityId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityScheduleRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            request.ToUpdateCommand(providerId, availabilityId),
            cancellationToken));

    private static async Task<IResult> DeactivateAvailabilityAsync(
        Guid providerId,
        Guid availabilityId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeactivateAvailabilityCommand(providerId, availabilityId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddAvailabilityExceptionAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityExceptionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddAvailabilityExceptionCommand(
                providerId,
                request.Date,
                request.IsUnavailable,
                request.OverrideStartTime,
                request.OverrideEndTime,
                request.Reason),
            cancellationToken);
        return Results.Created(
            $"/api/providers/{providerId}/availability/exceptions/{result.Id}",
            result);
    }

    private static async Task<IResult> RemoveAvailabilityExceptionAsync(
        Guid providerId,
        Guid exceptionId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RemoveAvailabilityExceptionCommand(providerId, exceptionId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAvailableSlotsAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery(Name = "from")] DateOnly fromDate,
        [FromQuery(Name = "to")] DateOnly toDate,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new GetAvailableSlotsQuery(providerId, fromDate, toDate),
            cancellationToken));
}

internal sealed record AvailabilityScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes = 30)
{
    public CreateAvailabilityCommand ToCreateCommand(Guid providerId) =>
        new(providerId, DayOfWeek, StartTime, EndTime, SlotDurationMinutes);

    public UpdateAvailabilityCommand ToUpdateCommand(
        Guid providerId,
        Guid availabilityId) =>
        new(
            providerId,
            availabilityId,
            DayOfWeek,
            StartTime,
            EndTime,
            SlotDurationMinutes);
}

internal sealed record AvailabilityExceptionRequest(
    DateOnly Date,
    bool IsUnavailable,
    TimeOnly? OverrideStartTime,
    TimeOnly? OverrideEndTime,
    string? Reason);
