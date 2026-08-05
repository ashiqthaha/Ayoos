using Ayoos.Domain.PracticeInvitations;

namespace Ayoos.Application.PracticeInvitations;

public sealed record CreatePracticeInvitationResult(
    Guid InvitationId,
    string SetupUrl);

public sealed record PracticeInvitationSummaryModel(
    Guid Id,
    string Email,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record PagedPracticeInvitationListModel(
    IReadOnlyList<PracticeInvitationSummaryModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PracticeInvitationSetupModel(
    string Email,
    string Status);

internal static class PracticeInvitationMappings
{
    public static PracticeInvitationSummaryModel ToSummaryModel(
        this PracticeInvitation invitation,
        DateTimeOffset now)
    {
        var status = invitation.Status == PracticeInvitationStatus.Pending
            && invitation.ExpiresAt <= now
                ? PracticeInvitationStatus.Expired
                : invitation.Status;

        return new PracticeInvitationSummaryModel(
            invitation.Id,
            invitation.Email,
            status.ToString(),
            invitation.ExpiresAt,
            invitation.CreatedAt);
    }
}
