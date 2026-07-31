using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Patients.LinkPatientToKeycloakUser;

public sealed record LinkPatientToKeycloakUserCommand(
    Guid PatientId,
    string KeycloakUserId) : IRequest<PatientModel>;

public sealed class LinkPatientToKeycloakUserCommandValidator
    : AbstractValidator<LinkPatientToKeycloakUserCommand>
{
    public LinkPatientToKeycloakUserCommandValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.KeycloakUserId).NotEmpty().MaximumLength(200);
    }
}

internal sealed class LinkPatientToKeycloakUserCommandHandler(
    IPatientRepository patientRepository)
    : IRequestHandler<LinkPatientToKeycloakUserCommand, PatientModel>
{
    public async Task<PatientModel> Handle(
        LinkPatientToKeycloakUserCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(
            request.PatientId,
            cancellationToken: cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException($"Patient '{request.PatientId}' was not found.");
        }

        var keycloakUserId = request.KeycloakUserId.Trim();
        if (await patientRepository.IsKeycloakUserLinkedAsync(
                keycloakUserId,
                patient.Id,
                cancellationToken))
        {
            throw new ConflictException(
                $"Keycloak user '{keycloakUserId}' is already linked to another patient.");
        }

        patient.LinkToKeycloakUser(keycloakUserId, DateTimeOffset.UtcNow);
        await patientRepository.SaveChangesAsync(cancellationToken);
        return patient.ToModel();
    }
}
