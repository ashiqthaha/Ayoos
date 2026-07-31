using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Patients;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Patients.UpdatePatient;

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly DateOfBirth,
    PatientSex Sex,
    string Email,
    string Phone,
    PatientAddressModel Address,
    string? PreferredLanguage,
    EmergencyContactInput? EmergencyContact) : IRequest<PatientModel>;

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
        PatientValidation.AddPatientRules(
            this,
            command => command.FirstName,
            command => command.LastName,
            command => command.PreferredName,
            command => command.DateOfBirth,
            command => command.Sex,
            command => command.Email,
            command => command.Phone,
            command => command.Address,
            command => command.PreferredLanguage,
            command => command.EmergencyContact);
    }
}

internal sealed class UpdatePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<UpdatePatientCommand, PatientModel>
{
    public async Task<PatientModel> Handle(
        UpdatePatientCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(
            request.PatientId,
            cancellationToken: cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException($"Patient '{request.PatientId}' was not found.");
        }

        var previousContact = patient.EmergencyContact;
        var replacementContact = request.EmergencyContact?.ToEntity(patient.Id);

        patient.Update(
            request.FirstName,
            request.LastName,
            request.PreferredName,
            request.DateOfBirth,
            request.Sex,
            request.Email,
            request.Phone,
            request.Address.ToValueObject(),
            request.PreferredLanguage,
            replacementContact,
            DateTimeOffset.UtcNow);

        if (previousContact is not null)
        {
            patientRepository.RemoveEmergencyContact(previousContact);
        }

        if (replacementContact is not null)
        {
            await patientRepository.AddEmergencyContactAsync(
                replacementContact,
                cancellationToken);
        }

        await patientRepository.SaveChangesAsync(cancellationToken);
        return patient.ToModel();
    }
}
