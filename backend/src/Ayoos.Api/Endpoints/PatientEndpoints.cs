using System.Security.Claims;
using Ayoos.Api.Authentication;
using Ayoos.Application.Patients;
using Ayoos.Application.Patients.DeactivatePatient;
using Ayoos.Application.Patients.GetPatient;
using Ayoos.Application.Patients.GetPatientByKeycloakUserId;
using Ayoos.Application.Patients.ListPatients;
using Ayoos.Application.Patients.RegisterPatient;
using Ayoos.Application.Patients.UpdatePatient;
using Ayoos.Domain.Patients;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ayoos.Api.Endpoints;

internal static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/patients")
            .WithTags("Patients")
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

        group.MapGet(string.Empty, ListPatientsAsync)
            .WithName("ListPatients")
            .WithSummary("Lists and searches patients in the current practice tenant.")
            .WithDescription("Search matches patient names, email addresses, and phone numbers. Page size is limited to 100.")
            .Produces<PagedPatientListModel>()
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapPost(string.Empty, RegisterPatientAsync)
            .WithName("RegisterPatient")
            .WithSummary("Registers a patient after an optional duplicate confirmation step.")
            .WithDescription("When a possible active match exists and confirmDuplicate is false, returns the matches without creating a patient. Repeat with confirmDuplicate true to proceed.")
            .Produces<RegisterPatientResult>(StatusCodes.Status200OK)
            .Produces<RegisterPatientResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapGet("/me", GetCurrentPatientAsync)
            .WithName("GetCurrentPatient")
            .WithSummary("Gets the patient record linked to the current Keycloak user.")
            .Produces<PatientModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.PatientOnly);

        group.MapGet("/{id:guid}", GetPatientAsync)
            .WithName("GetPatient")
            .WithSummary("Gets a patient in the current practice tenant.")
            .Produces<PatientModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapPut("/{id:guid}", UpdatePatientAsync)
            .WithName("UpdatePatient")
            .WithSummary("Updates a patient's demographics and contacts.")
            .Produces<PatientModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        group.MapPost("/{id:guid}/deactivate", DeactivatePatientAsync)
            .WithName("DeactivatePatient")
            .WithSummary("Deactivates a patient while retaining their record.")
            .Produces<PatientModel>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthorizationPolicies.StaffOrAdmin);

        return endpoints;
    }

    private static async Task<IResult> ListPatientsAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(
                new ListPatientsQuery(
                    search,
                    page <= 0 ? 1 : page,
                    pageSize <= 0 ? 20 : pageSize),
                cancellationToken));

    private static async Task<IResult> RegisterPatientAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        PatientRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToRegisterCommand(), cancellationToken);
        return result.Patient is null
            ? Results.Ok(result)
            : Results.Created($"/api/patients/{result.Patient.Id}", result);
    }

    private static async Task<IResult> GetCurrentPatientAsync(
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var keycloakUserId = user.FindFirst("sub")?.Value;
        var result = string.IsNullOrWhiteSpace(keycloakUserId)
            ? null
            : await sender.Send(
                new GetPatientByKeycloakUserIdQuery(keycloakUserId),
                cancellationToken);

        return result is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Patient record not linked.",
                detail: "No patient record is linked to the authenticated user.")
            : Results.Ok(result);
    }

    private static async Task<IResult> GetPatientAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPatientQuery(id), cancellationToken);
        return result is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Patient not found.")
            : Results.Ok(result);
    }

    private static async Task<IResult> UpdatePatientAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        PatientRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(request.ToUpdateCommand(id), cancellationToken));

    private static async Task<IResult> DeactivatePatientAsync(
        Guid id,
        [FromHeader(Name = "X-Tenant")] string? _tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(
            await sender.Send(new DeactivatePatientCommand(id), cancellationToken));
}

internal sealed record PatientRequest(
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly DateOfBirth,
    PatientSex Sex,
    string Email,
    string Phone,
    PatientAddressModel Address,
    string? PreferredLanguage,
    EmergencyContactInput? EmergencyContact,
    bool ConfirmDuplicate = false)
{
    public RegisterPatientCommand ToRegisterCommand() =>
        new(
            FirstName,
            LastName,
            PreferredName,
            DateOfBirth,
            Sex,
            Email,
            Phone,
            Address,
            PreferredLanguage,
            EmergencyContact,
            ConfirmDuplicate);

    public UpdatePatientCommand ToUpdateCommand(Guid patientId) =>
        new(
            patientId,
            FirstName,
            LastName,
            PreferredName,
            DateOfBirth,
            Sex,
            Email,
            Phone,
            Address,
            PreferredLanguage,
            EmergencyContact);
}
