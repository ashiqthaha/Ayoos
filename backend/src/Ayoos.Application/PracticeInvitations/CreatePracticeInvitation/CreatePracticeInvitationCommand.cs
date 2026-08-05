using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Common.Security;
using Ayoos.Domain.PracticeInvitations;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ayoos.Application.PracticeInvitations.CreatePracticeInvitation;

public sealed record CreatePracticeInvitationCommand(
    string Email,
    int ExpiryDays = 7) : IRequest<CreatePracticeInvitationResult>;

public sealed class CreatePracticeInvitationCommandValidator
    : AbstractValidator<CreatePracticeInvitationCommand>
{
    public CreatePracticeInvitationCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();

        RuleFor(command => command.ExpiryDays)
            .InclusiveBetween(1, 30);
    }
}

internal sealed class CreatePracticeInvitationCommandHandler(
    IPracticeInvitationRepository invitationRepository,
    IPracticeInvitationUserService invitationUserService,
    ICurrentUserContext currentUserContext,
    IFrontendUrlProvider frontendUrlProvider,
    TimeProvider timeProvider,
    ILogger<CreatePracticeInvitationCommandHandler> logger)
    : IRequestHandler<CreatePracticeInvitationCommand, CreatePracticeInvitationResult>
{
    public async Task<CreatePracticeInvitationResult> Handle(
        CreatePracticeInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var creatorSubject = currentUserContext.KeycloakSubject;
        if (string.IsNullOrWhiteSpace(creatorSubject))
        {
            throw new ForbiddenException(
                "An authenticated Ayoos super-admin is required to create an invitation.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        string? createdKeycloakUserId = null;

        try
        {
            createdKeycloakUserId = await invitationUserService.CreatePracticeAdminAsync(
                email,
                cancellationToken);
            var rawToken = PracticeInvitationToken.Generate();
            var now = timeProvider.GetUtcNow();
            var invitation = PracticeInvitation.Create(
                PracticeInvitationToken.Hash(rawToken),
                email,
                createdKeycloakUserId,
                now.AddDays(request.ExpiryDays),
                creatorSubject,
                now);
            var setupUrl = frontendUrlProvider.BuildPracticeSetupUrl(rawToken);

            await invitationRepository.AddAsync(invitation, cancellationToken);
            await invitationRepository.SaveChangesAsync(cancellationToken);

            return new CreatePracticeInvitationResult(invitation.Id, setupUrl);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(createdKeycloakUserId))
            {
                try
                {
                    await invitationUserService.DeleteUserAsync(
                        createdKeycloakUserId,
                        CancellationToken.None);
                }
                catch (Exception compensationException)
                {
                    logger.LogError(
                        compensationException,
                        "Failed to delete orphaned Keycloak user {KeycloakUserId} after practice invitation creation failed.",
                        createdKeycloakUserId);
                }
            }

            throw;
        }
    }
}
