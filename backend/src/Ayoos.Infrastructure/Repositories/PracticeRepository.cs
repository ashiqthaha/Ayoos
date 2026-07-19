using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Practices;
using Ayoos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Repositories;

internal sealed class PracticeRepository(AyoosDbContext dbContext)
    : IPracticeRepository
{
    public Task<Practice?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default) =>
        dbContext.Practices.SingleOrDefaultAsync(
            practice => practice.Slug == slug,
            cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
