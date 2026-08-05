using Ayoos.Domain.PracticeInvitations;

namespace Ayoos.Application.Common.Interfaces;

public interface IPracticeInvitationRepository
{
    Task AddAsync(
        PracticeInvitation invitation,
        CancellationToken cancellationToken = default);

    Task<PracticeInvitation?> GetByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    Task<PracticeInvitation?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PracticeInvitation> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
