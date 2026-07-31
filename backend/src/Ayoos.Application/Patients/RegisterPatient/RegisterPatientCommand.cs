using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Patients;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Patients.RegisterPatient;

public sealed record RegisterPatientCommand(
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly DateOfBirth,
    PatientSex Sex,
    string Email,
    string Phone,
    PatientAddressModel Address,
    string? PreferredLanguage,
    EmergencyContactInput? EmergencyContact,
    bool ConfirmDuplicate = false) : IRequest<RegisterPatientResult>;

public sealed class RegisterPatientCommandValidator
    : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
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

internal sealed class RegisterPatientCommandHandler(
    IPatientRepository patientRepository,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<RegisterPatientCommand, RegisterPatientResult>
{
    public async Task<RegisterPatientResult> Handle(
        RegisterPatientCommand request,
        CancellationToken cancellationToken)
    {
        var possibleMatches = await patientRepository.FindActiveDuplicatesAsync(
            currentPractice.PracticeId,
            request.LastName.Trim(),
            request.DateOfBirth,
            cancellationToken: cancellationToken);
        var duplicateModels = possibleMatches
            .Select(patient => patient.ToDuplicateModel())
            .ToArray();

        if (duplicateModels.Length > 0 && !request.ConfirmDuplicate)
        {
            return new RegisterPatientResult(null, true, duplicateModels);
        }

        var now = DateTimeOffset.UtcNow;
        var patient = Patient.Create(
            currentPractice.PracticeId,
            request.FirstName,
            request.LastName,
            request.PreferredName,
            request.DateOfBirth,
            request.Sex,
            request.Email,
            request.Phone,
            request.Address.ToValueObject(),
            request.PreferredLanguage,
            now);

        if (request.EmergencyContact is not null)
        {
            patient.ReplaceEmergencyContact(
                request.EmergencyContact.ToEntity(patient.Id));
        }

        await patientRepository.AddAsync(patient, cancellationToken);
        await patientRepository.SaveChangesAsync(cancellationToken);

        return new RegisterPatientResult(
            patient.ToModel(),
            duplicateModels.Length > 0,
            duplicateModels);
    }
}
