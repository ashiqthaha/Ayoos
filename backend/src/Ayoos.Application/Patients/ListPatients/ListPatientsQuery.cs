using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Patients.ListPatients;

public sealed record ListPatientsQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedPatientListModel>;

internal sealed class ListPatientsQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<ListPatientsQuery, PagedPatientListModel>
{
    public async Task<PagedPatientListModel> Handle(
        ListPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await patientRepository.ListAsync(
            request.Search,
            page,
            pageSize,
            cancellationToken);

        return new PagedPatientListModel(
            result.Items.Select(patient => patient.ToModel()).ToArray(),
            page,
            pageSize,
            result.TotalCount,
            result.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(result.TotalCount / (double)pageSize));
    }
}
