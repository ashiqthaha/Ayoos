using System.Security.Claims;
using Ayoos.Api.Authentication;
using Ayoos.Application.Bookings;
using Ayoos.Application.Bookings.CancelBooking;
using Ayoos.Application.Bookings.CompleteBooking;
using Ayoos.Application.Bookings.ConfirmBooking;
using Ayoos.Application.Bookings.CreateBooking;
using Ayoos.Application.Bookings.GetBooking;
using Ayoos.Application.Bookings.GetProviderSchedule;
using Ayoos.Application.Bookings.ListBookings;
using Ayoos.Application.Bookings.MarkNoShow;
using Ayoos.Application.Patients.GetPatientByKeycloakUserId;
using Ayoos.Domain.Bookings;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ayoos.Api.Endpoints;

internal static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bookings")
            .WithTags("Bookings")
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

        group.MapPost(string.Empty, CreateBookingAsync)
            .WithName("CreateBooking")
            .WithSummary("Requests a booking for an exact computed provider slot.")
            .WithDescription("Patients can request bookings only for their linked patient record. Staff and practice administrators can request on a patient's behalf.")
            .Produces<BookingModel>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "staff", "practice-admin"));

        group.MapGet(string.Empty, ListBookingsAsync)
            .WithName("ListBookings")
            .WithSummary("Lists bookings with provider, patient, date, status, and paging filters.")
            .WithDescription("Patient users are always restricted to their own linked patient record.")
            .Produces<PagedBookingListModel>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/provider-schedule", GetProviderScheduleAsync)
            .WithName("GetProviderBookingSchedule")
            .WithSummary("Gets a provider's bookings over an inclusive date range.")
            .Produces<IReadOnlyList<BookingModel>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapGet("/{id:guid}", GetBookingAsync)
            .WithName("GetBooking")
            .WithSummary("Gets a booking in the current practice tenant.")
            .WithDescription("Patient users may retrieve only their own bookings.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/confirm", ConfirmBookingAsync)
            .WithName("ConfirmBooking")
            .WithSummary("Confirms a requested booking.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapPost("/{id:guid}/cancel", CancelBookingAsync)
            .WithName("CancelBooking")
            .WithSummary("Cancels a requested or confirmed booking.")
            .WithDescription("Patients can cancel only their own bookings. Staff and practice administrators can cancel any booking in the tenant.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "staff", "practice-admin"));

        group.MapPost("/{id:guid}/complete", CompleteBookingAsync)
            .WithName("CompleteBooking")
            .WithSummary("Marks a confirmed booking complete.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        group.MapPost("/{id:guid}/no-show", MarkNoShowAsync)
            .WithName("MarkBookingNoShow")
            .WithSummary("Marks a confirmed booking as a no-show.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        return endpoints;
    }

    private static async Task<IResult> CreateBookingAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        CreateBookingRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (IsPatientActor(user))
        {
            var patientId = await GetLinkedPatientIdAsync(
                user,
                sender,
                cancellationToken);
            if (!patientId.HasValue || patientId.Value != request.PatientId)
            {
                return Forbidden(
                    "Patients can create bookings only for their linked patient record.");
            }
        }

        var result = await sender.Send(request.ToCommand(), cancellationToken);
        return Results.Created($"/api/bookings/{result.Id}", result);
    }

    private static async Task<IResult> ListBookingsAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery] Guid? providerId,
        [FromQuery] Guid? patientId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] BookingStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (IsPatientActor(user))
        {
            patientId = await GetLinkedPatientIdAsync(user, sender, cancellationToken);
            if (!patientId.HasValue)
            {
                return Forbidden("No patient record is linked to the signed-in user.");
            }
        }

        var result = await sender.Send(
            new ListBookingsQuery(
                providerId,
                patientId,
                fromDate,
                toDate,
                status,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBookingAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingQuery(id), cancellationToken);
        if (result is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Booking not found.");
        }

        if (IsPatientActor(user))
        {
            var patientId = await GetLinkedPatientIdAsync(user, sender, cancellationToken);
            if (!patientId.HasValue || patientId.Value != result.PatientId)
            {
                return Forbidden("Patients can view only their own bookings.");
            }
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetProviderScheduleAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery] Guid providerId,
        [FromQuery(Name = "from")] DateOnly fromDate,
        [FromQuery(Name = "to")] DateOnly toDate,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new GetProviderScheduleQuery(providerId, fromDate, toDate),
            cancellationToken));

    private static async Task<IResult> ConfirmBookingAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new ConfirmBookingCommand(id),
            cancellationToken));

    private static async Task<IResult> CancelBookingAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (IsPatientActor(user))
        {
            var booking = await sender.Send(new GetBookingQuery(id), cancellationToken);
            if (booking is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Booking not found.");
            }

            var patientId = await GetLinkedPatientIdAsync(user, sender, cancellationToken);
            if (!patientId.HasValue || patientId.Value != booking.PatientId)
            {
                return Forbidden("Patients can cancel only their own bookings.");
            }
        }

        return Results.Ok(await sender.Send(
            new CancelBookingCommand(id),
            cancellationToken));
    }

    private static async Task<IResult> CompleteBookingAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new CompleteBookingCommand(id),
            cancellationToken));

    private static async Task<IResult> MarkNoShowAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new MarkNoShowCommand(id),
            cancellationToken));

    private static bool IsPatientActor(ClaimsPrincipal user) =>
        user.IsInRole("patient") &&
        !user.IsInRole("staff") &&
        !user.IsInRole("provider") &&
        !user.IsInRole("practice-admin");

    private static async Task<Guid?> GetLinkedPatientIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var subject = user.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var patient = await sender.Send(
            new GetPatientByKeycloakUserIdQuery(subject),
            cancellationToken);
        return patient?.Id;
    }

    private static IResult Forbidden(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden.",
            detail: detail);
}

internal sealed record CreateBookingRequest(
    Guid PatientId,
    Guid ProviderId,
    Guid? AvailabilityScheduleId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Reason)
{
    public CreateBookingCommand ToCommand() =>
        new(
            PatientId,
            ProviderId,
            AvailabilityScheduleId,
            StartTime,
            EndTime,
            Reason);
}
