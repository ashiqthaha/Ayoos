using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Patients.GetPatient;

public sealed record GetPatientQuery(Guid PatientId) : IRequest<PatientModel?>;

internal sealed class GetPatientQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetPatientQuery, PatientModel?>
{
    public async Task<PatientModel?> Handle(
        GetPatientQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(
            request.PatientId,
            cancellationToken: cancellationToken);
        return patient?.ToModel();
    }
}
