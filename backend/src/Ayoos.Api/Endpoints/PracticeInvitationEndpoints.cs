using Ayoos.Api.Authentication;
using Ayoos.Application.PracticeInvitations;
using Ayoos.Application.PracticeInvitations.CreatePracticeInvitation;
using Ayoos.Application.PracticeInvitations.GetInvitationByToken;
using Ayoos.Application.PracticeInvitations.ListPracticeInvitations;
using Ayoos.Application.PracticeInvitations.RevokePracticeInvitation;
using MediatR;

namespace Ayoos.Api.Endpoints;

internal static class PracticeInvitationEndpoints
{
    public static IEndpointRouteBuilder MapPracticeInvitationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/api/admin/invitations")
            .WithTags("Practice invitations")
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin);

        adminGroup.MapPost(string.Empty, CreateInvitationAsync)
            .WithName("CreatePracticeInvitation")
            .WithSummary("Creates an invited practice-admin and a single-use setup URL.")
            .Produces<CreatePracticeInvitationResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        adminGroup.MapGet(string.Empty, ListInvitationsAsync)
            .WithName("ListPracticeInvitations")
            .WithSummary("Lists practice invitations without exposing token material.")
            .Produces<PagedPracticeInvitationListModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        adminGroup.MapPost("/{id:guid}/revoke", RevokeInvitationAsync)
            .WithName("RevokePracticeInvitation")
            .WithSummary("Revokes a pending practice invitation.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        endpoints.MapGet(
                "/api/setup/invitations/{token}",
                GetInvitationByTokenAsync)
            .WithTags("Practice setup")
            .WithName("GetPracticeInvitationByToken")
            .WithSummary("Checks whether a setup token routes to a pending invitation.")
            .AllowAnonymous()
            .Produces<PracticeInvitationSetupModel>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        return endpoints;
    }

    private static async Task<IResult> CreateInvitationAsync(
        CreatePracticeInvitationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePracticeInvitationCommand(request.Email, request.ExpiryDays ?? 7),
            cancellationToken);

        return Results.Created(
            $"/api/admin/invitations/{result.InvitationId}",
            result);
    }

    private static async Task<IResult> ListInvitationsAsync(
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ListPracticeInvitationsQuery(
                page ?? 1,
                pageSize ?? 20),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RevokePracticeInvitationCommand(id),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetInvitationByTokenAsync(
        string token,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetInvitationByTokenQuery(token),
            cancellationToken);
        return Results.Ok(result);
    }
}

internal sealed record CreatePracticeInvitationRequest(
    string Email,
    int? ExpiryDays);
