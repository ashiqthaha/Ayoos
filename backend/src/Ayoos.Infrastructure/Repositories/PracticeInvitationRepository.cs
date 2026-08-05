using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.PracticeInvitations;
using Ayoos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Repositories;

internal sealed class PracticeInvitationRepository(AyoosDbContext dbContext)
    : IPracticeInvitationRepository
{
    public async Task AddAsync(
        PracticeInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PracticeInvitations.AddAsync(invitation, cancellationToken);
    }

    public Task<PracticeInvitation?> GetByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default) =>
        dbContext.PracticeInvitations.SingleOrDefaultAsync(
            invitation => invitation.Id == invitationId,
            cancellationToken);

    public Task<PracticeInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        dbContext.PracticeInvitations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                invitation => invitation.TokenHash == tokenHash,
                cancellationToken);

    public async Task<(IReadOnlyList<PracticeInvitation> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PracticeInvitations
            .AsNoTracking()
            .OrderByDescending(invitation => invitation.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
