using System.Security.Claims;
using Ayoos.Api.Authentication;
using Ayoos.Application.Bookings;
using Ayoos.Application.Bookings.CancelBookingByPatient;
using Ayoos.Application.Bookings.CancelBookingByProvider;
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
        var bookings = endpoints.MapGroup("/api/bookings")
            .WithTags("Bookings")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .AddEndpointFilter(RequireTenantAsync);

        bookings.MapPost(string.Empty, CreateBookingAsync)
            .WithName("CreateBooking")
            .WithSummary("Creates a pending booking or previews active overlap conflicts.")
            .WithDescription("The requested UTC interval must exactly match a generated, non-excepted provider slot. Repeat with force=true only after explicitly acknowledging the returned conflicts.")
            .Produces<CreateBookingResult>(StatusCodes.Status200OK)
            .Produces<CreateBookingResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "provider", "practice-admin"));

        bookings.MapGet(string.Empty, ListBookingsAsync)
            .WithName("ListBookings")
            .WithSummary("Lists bookings with provider, patient, status, date, and paging filters.")
            .WithDescription("Patients are restricted to bookings for their linked patient record.")
            .Produces<PagedBookingListModel>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "provider", "practice-admin"));

        bookings.MapGet("/{id:guid}", GetBookingAsync)
            .WithName("GetBookingById")
            .WithSummary("Gets one tenant-scoped booking.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "provider", "practice-admin"));

        bookings.MapPost("/{id:guid}/confirm", ConfirmBookingAsync)
            .WithName("ConfirmBooking")
            .WithSummary("Confirms a pending booking after rechecking active overlaps.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        bookings.MapPost("/{id:guid}/cancel-patient", CancelBookingByPatientAsync)
            .WithName("CancelBookingByPatient")
            .WithSummary("Cancels a pending or confirmed booking as the patient.")
            .WithDescription("Patients may cancel only their own bookings. Providers and practice administrators may manage any booking in the tenant.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "provider", "practice-admin"));

        bookings.MapPost("/{id:guid}/cancel-provider", CancelBookingByProviderAsync)
            .WithName("CancelBookingByProvider")
            .WithSummary("Cancels a pending or confirmed booking as the provider.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        bookings.MapPost("/{id:guid}/complete", CompleteBookingAsync)
            .WithName("CompleteBooking")
            .WithSummary("Marks a confirmed booking complete.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        bookings.MapPost("/{id:guid}/no-show", MarkNoShowAsync)
            .WithName("MarkBookingNoShow")
            .WithSummary("Marks a confirmed booking as a no-show.")
            .Produces<BookingModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthorizationPolicies.ProviderOrAdmin);

        endpoints.MapGet(
                "/api/providers/{providerId:guid}/schedule",
                GetProviderScheduleAsync)
            .WithTags("Bookings")
            .WithName("GetProviderSchedule")
            .WithSummary("Gets booked appointments and generated open slots over an inclusive date range.")
            .WithDescription("Patient responses omit booking details and expose only open slots.")
            .Produces<ProviderScheduleModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy =>
                policy.RequireRole("patient", "provider", "practice-admin"))
            .AddEndpointFilter(RequireTenantAsync);

        return endpoints;
    }

    private static async ValueTask<object?> RequireTenantAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
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
        return result.Booking is null
            ? Results.Ok(result)
            : Results.Created($"/api/bookings/{result.Booking.Id}", result);
    }

    private static async Task<IResult> ListBookingsAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery] Guid? providerId,
        [FromQuery] Guid? patientId,
        [FromQuery(Name = "from")] DateOnly? fromDate,
        [FromQuery(Name = "to")] DateOnly? toDate,
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
        var result = await sender.Send(new GetBookingByIdQuery(id), cancellationToken);
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
        Guid providerId,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery(Name = "from")] DateOnly fromDate,
        [FromQuery(Name = "to")] DateOnly toDate,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetProviderScheduleQuery(providerId, fromDate, toDate),
            cancellationToken);
        return Results.Ok(IsPatientActor(user)
            ? result with { Bookings = [] }
            : result);
    }

    private static async Task<IResult> ConfirmBookingAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new ConfirmBookingCommand(id),
            cancellationToken));

    private static async Task<IResult> CancelBookingByPatientAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        CancellationRequest? request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (IsPatientActor(user))
        {
            var booking = await sender.Send(
                new GetBookingByIdQuery(id),
                cancellationToken);
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
            new CancelBookingByPatientCommand(id, request?.CancellationReason),
            cancellationToken));
    }

    private static async Task<IResult> CancelBookingByProviderAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        CancellationRequest? request,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new CancelBookingByProviderCommand(id, request?.CancellationReason),
            cancellationToken));

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
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd,
    string? Reason,
    bool Force = false)
{
    public CreateBookingCommand ToCommand() =>
        new(
            PatientId,
            ProviderId,
            AvailabilityScheduleId,
            ScheduledStart,
            ScheduledEnd,
            Reason,
            Force);
}

internal sealed record CancellationRequest(string? CancellationReason = null);
