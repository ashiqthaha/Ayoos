using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.PracticeInvitations.GetInvitationByToken;

public sealed record GetInvitationByTokenQuery(
    string RawToken) : IRequest<PracticeInvitationSetupModel>;

public sealed class GetInvitationByTokenQueryValidator
    : AbstractValidator<GetInvitationByTokenQuery>
{
    public GetInvitationByTokenQueryValidator()
    {
        RuleFor(query => query.RawToken)
            .NotEmpty()
            .MaximumLength(200);
    }
}

internal sealed class GetInvitationByTokenQueryHandler(
    IPracticeInvitationRepository invitationRepository,
    TimeProvider timeProvider)
    : IRequestHandler<GetInvitationByTokenQuery, PracticeInvitationSetupModel>
{
    public async Task<PracticeInvitationSetupModel> Handle(
        GetInvitationByTokenQuery request,
        CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(
            PracticeInvitationToken.Hash(request.RawToken),
            cancellationToken)
            ?? throw new NotFoundException("This practice invitation was not found.");

        if (!invitation.IsUsable(timeProvider.GetUtcNow()))
        {
            throw new GoneException("This practice invitation is no longer valid.");
        }

        return new PracticeInvitationSetupModel(
            invitation.Email,
            invitation.Status.ToString());
    }
}
