using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.PracticeInvitations.ListPracticeInvitations;

public sealed record ListPracticeInvitationsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<PagedPracticeInvitationListModel>;

public sealed class ListPracticeInvitationsQueryValidator
    : AbstractValidator<ListPracticeInvitationsQuery>
{
    public ListPracticeInvitationsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class ListPracticeInvitationsQueryHandler(
    IPracticeInvitationRepository invitationRepository,
    TimeProvider timeProvider)
    : IRequestHandler<ListPracticeInvitationsQuery, PagedPracticeInvitationListModel>
{
    public async Task<PagedPracticeInvitationListModel> Handle(
        ListPracticeInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await invitationRepository.ListAsync(
            request.Page,
            request.PageSize,
            cancellationToken);
        var now = timeProvider.GetUtcNow();

        return new PagedPracticeInvitationListModel(
            result.Items.Select(invitation => invitation.ToSummaryModel(now)).ToArray(),
            request.Page,
            request.PageSize,
            result.TotalCount,
            result.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(result.TotalCount / (double)request.PageSize));
    }
}
