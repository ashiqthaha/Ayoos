using Ayoos.Domain.Practices;

namespace Ayoos.Application.Common.Interfaces;

public interface IPracticeRepository
{
    Task<Practice?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
