using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Common.Security;
using Ayoos.Domain.Practices;
using MediatR;

namespace Ayoos.Application.Practices.CreatePractice;

internal sealed class CreatePracticeCommandHandler(
    IPracticeProvisioner practiceProvisioner,
    ITenantRegistry tenantRegistry,
    IPracticeInvitationRepository invitationRepository,
    ICurrentUserContext currentUserContext,
    TimeProvider timeProvider)
    : IRequestHandler<CreatePracticeCommand, PracticeModel>
{
    public async Task<PracticeModel> Handle(
        CreatePracticeCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(
            PracticeInvitationToken.Hash(request.RawToken),
            cancellationToken)
            ?? throw new NotFoundException("This practice invitation was not found.");
        var now = timeProvider.GetUtcNow();

        if (!invitation.IsUsable(now))
        {
            throw new GoneException("This practice invitation is no longer valid.");
        }

        if (!string.Equals(
                currentUserContext.KeycloakSubject,
                invitation.PracticeAdminKeycloakUserId,
                StringComparison.Ordinal))
        {
            throw new ForbiddenException(
                "The signed-in Keycloak user does not match this practice invitation.");
        }

        if (await tenantRegistry.IdentifierExistsAsync(request.Slug, cancellationToken))
        {
            throw new ConflictException(
                $"A practice with slug '{request.Slug}' already exists.");
        }

        var practice = Practice.Create(
            request.Name,
            request.Slug,
            request.TimeZone,
            request.Address.ToValueObject(),
            request.ContactEmail,
            request.ContactPhone,
            now);

        await practiceProvisioner.ProvisionAsync(
            practice,
            invitation.Id,
            invitation.PracticeAdminKeycloakUserId,
            now,
            cancellationToken);

        return practice.ToModel();
    }
}
