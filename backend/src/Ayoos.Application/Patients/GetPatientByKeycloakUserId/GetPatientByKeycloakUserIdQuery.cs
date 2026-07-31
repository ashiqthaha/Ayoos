using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Patients.GetPatientByKeycloakUserId;

public sealed record GetPatientByKeycloakUserIdQuery(string KeycloakUserId)
    : IRequest<PatientModel?>;

internal sealed class GetPatientByKeycloakUserIdQueryHandler(
    IPatientRepository patientRepository)
    : IRequestHandler<GetPatientByKeycloakUserIdQuery, PatientModel?>
{
    public async Task<PatientModel?> Handle(
        GetPatientByKeycloakUserIdQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByKeycloakUserIdAsync(
            request.KeycloakUserId,
            cancellationToken);
        return patient?.ToModel();
    }
}
