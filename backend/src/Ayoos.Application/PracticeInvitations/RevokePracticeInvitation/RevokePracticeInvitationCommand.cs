using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.PracticeInvitations;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.PracticeInvitations.RevokePracticeInvitation;

public sealed record RevokePracticeInvitationCommand(Guid InvitationId) : IRequest;

public sealed class RevokePracticeInvitationCommandValidator
    : AbstractValidator<RevokePracticeInvitationCommand>
{
    public RevokePracticeInvitationCommandValidator()
    {
        RuleFor(command => command.InvitationId).NotEmpty();
    }
}

internal sealed class RevokePracticeInvitationCommandHandler(
    IPracticeInvitationRepository invitationRepository,
    TimeProvider timeProvider)
    : IRequestHandler<RevokePracticeInvitationCommand>
{
    public async Task Handle(
        RevokePracticeInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByIdAsync(
            request.InvitationId,
            cancellationToken)
            ?? throw new NotFoundException(
                $"Practice invitation '{request.InvitationId}' was not found.");

        var now = timeProvider.GetUtcNow();
        if (!invitation.IsUsable(now))
        {
            invitation.Expire(now);
            await invitationRepository.SaveChangesAsync(cancellationToken);
            throw new GoneException("This practice invitation is no longer pending.");
        }

        invitation.Revoke();
        await invitationRepository.SaveChangesAsync(cancellationToken);
    }
}
