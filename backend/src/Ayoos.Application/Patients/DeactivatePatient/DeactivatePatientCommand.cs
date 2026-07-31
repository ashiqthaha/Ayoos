using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Patients.DeactivatePatient;

public sealed record DeactivatePatientCommand(Guid PatientId) : IRequest<PatientModel>;

public sealed class DeactivatePatientCommandValidator
    : AbstractValidator<DeactivatePatientCommand>
{
    public DeactivatePatientCommandValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
    }
}

internal sealed class DeactivatePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<DeactivatePatientCommand, PatientModel>
{
    public async Task<PatientModel> Handle(
        DeactivatePatientCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(
            request.PatientId,
            cancellationToken: cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException($"Patient '{request.PatientId}' was not found.");
        }

        patient.Deactivate(DateTimeOffset.UtcNow);
        await patientRepository.SaveChangesAsync(cancellationToken);
        return patient.ToModel();
    }
}
