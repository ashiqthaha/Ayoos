using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Practices.GetPractice;

internal sealed class GetPracticeQueryHandler(IPracticeRepository practiceRepository)
    : IRequestHandler<GetPracticeQuery, PracticeModel?>
{
    public async Task<PracticeModel?> Handle(
        GetPracticeQuery request,
        CancellationToken cancellationToken)
    {
        var practice = await practiceRepository.GetBySlugAsync(
            request.Slug,
            cancellationToken);

        return practice?.ToModel();
    }
}
