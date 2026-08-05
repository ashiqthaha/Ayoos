using Ayoos.Api.Authentication;
using Ayoos.Application.Providers;
using Ayoos.Application.Providers.CreateAvailabilityException;
using Ayoos.Application.Providers.CreateAvailabilitySchedule;
using Ayoos.Application.Providers.DeleteAvailabilityException;
using Ayoos.Application.Providers.DeleteAvailabilitySchedule;
using Ayoos.Application.Providers.GenerateSlotPreview;
using Ayoos.Application.Providers.GetProviderExceptions;
using Ayoos.Application.Providers.GetProviderWeeklySchedule;
using Ayoos.Application.Providers.PreviewScheduleOverlap;
using Ayoos.Application.Providers.UpdateAvailabilitySchedule;
using Ayoos.Domain.Providers;
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

        group.MapGet(string.Empty, GetWeeklyScheduleAsync)
            .WithName("GetProviderWeeklySchedule")
            .WithSummary("Gets active weekly availability grouped from Monday through Sunday.")
            .Produces<ProviderWeeklyScheduleModel>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(string.Empty, CreateScheduleAsync)
            .WithName("CreateAvailabilitySchedule")
            .WithSummary("Creates a weekly window or returns overlap conflicts for confirmation.")
            .Produces<AvailabilityScheduleMutationResult>(StatusCodes.Status200OK)
            .Produces<AvailabilityScheduleMutationResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapPut("/{scheduleId:guid}", UpdateScheduleAsync)
            .WithName("UpdateAvailabilitySchedule")
            .WithSummary("Updates a weekly window or returns overlap conflicts for confirmation.")
            .Produces<AvailabilityScheduleMutationResult>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapDelete("/{scheduleId:guid}", DeleteScheduleAsync)
            .WithName("DeleteAvailabilitySchedule")
            .WithSummary("Deactivates a weekly availability window.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/exceptions", GetExceptionsAsync)
            .WithName("GetProviderExceptions")
            .WithSummary("Gets date-specific exceptions in an inclusive date range.")
            .Produces<IReadOnlyList<AvailabilityExceptionModel>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/exceptions", CreateExceptionAsync)
            .WithName("CreateAvailabilityException")
            .WithSummary("Creates an unavailable date or custom-hours override.")
            .Produces<AvailabilityExceptionModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapDelete("/exceptions/{exceptionId:guid}", DeleteExceptionAsync)
            .WithName("DeleteAvailabilityException")
            .WithSummary("Deletes a date-specific availability exception.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/overlap-preview", PreviewOverlapAsync)
            .WithName("PreviewScheduleOverlap")
            .WithSummary("Previews active weekly windows that overlap a candidate window.")
            .Produces<ScheduleOverlapPreviewModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/slots", GenerateSlotsAsync)
            .WithName("GenerateSlotPreview")
            .WithSummary("Materializes bookable slots for an inclusive date range.")
            .Produces<IReadOnlyList<AvailabilitySlotModel>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetWeeklyScheduleAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new GetProviderWeeklyScheduleQuery(providerId),
            cancellationToken));

    private static async Task<IResult> CreateScheduleAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityScheduleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCreateCommand(providerId), cancellationToken);
        return result.Schedule is null
            ? Results.Ok(result)
            : Results.Created(
                $"/api/providers/{providerId}/availability/{result.Schedule.Id}",
                result);
    }

    private static async Task<IResult> UpdateScheduleAsync(
        Guid providerId,
        Guid scheduleId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityScheduleRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            request.ToUpdateCommand(providerId, scheduleId),
            cancellationToken));

    private static async Task<IResult> DeleteScheduleAsync(
        Guid providerId,
        Guid scheduleId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteAvailabilityScheduleCommand(providerId, scheduleId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetExceptionsAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery(Name = "from")] DateOnly fromDate,
        [FromQuery(Name = "to")] DateOnly toDate,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new GetProviderExceptionsQuery(providerId, fromDate, toDate),
            cancellationToken));

    private static async Task<IResult> CreateExceptionAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        AvailabilityExceptionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAvailabilityExceptionCommand(
                providerId,
                request.Date,
                request.ExceptionType,
                request.StartTime,
                request.EndTime,
                request.Reason),
            cancellationToken);
        return Results.Created(
            $"/api/providers/{providerId}/availability/exceptions/{result.Id}",
            result);
    }

    private static async Task<IResult> DeleteExceptionAsync(
        Guid providerId,
        Guid exceptionId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteAvailabilityExceptionCommand(providerId, exceptionId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PreviewOverlapAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery] DayOfWeek dayOfWeek,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] int slotDurationMinutes,
        [FromQuery] Guid? excludeScheduleId,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new PreviewScheduleOverlapQuery(
                providerId,
                dayOfWeek,
                startTime,
                endTime,
                slotDurationMinutes,
                excludeScheduleId),
            cancellationToken));

    private static async Task<IResult> GenerateSlotsAsync(
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery(Name = "from")] DateOnly fromDate,
        [FromQuery(Name = "to")] DateOnly toDate,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new GenerateSlotPreviewQuery(providerId, fromDate, toDate),
            cancellationToken));
}

internal sealed record AvailabilityScheduleRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes = 30,
    bool ConfirmOverlap = false)
{
    public CreateAvailabilityScheduleCommand ToCreateCommand(Guid providerId) =>
        new(
            providerId,
            DayOfWeek,
            StartTime,
            EndTime,
            SlotDurationMinutes,
            ConfirmOverlap);

    public UpdateAvailabilityScheduleCommand ToUpdateCommand(
        Guid providerId,
        Guid scheduleId) =>
        new(
            providerId,
            scheduleId,
            DayOfWeek,
            StartTime,
            EndTime,
            SlotDurationMinutes,
            ConfirmOverlap);
}

internal sealed record AvailabilityExceptionRequest(
    DateOnly Date,
    AvailabilityExceptionType ExceptionType,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Reason);
