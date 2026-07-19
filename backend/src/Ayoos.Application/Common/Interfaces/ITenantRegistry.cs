namespace Ayoos.Application.Common.Interfaces;

public interface ITenantRegistry
{
    Task<bool> IdentifierExistsAsync(
        string identifier,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid practiceId,
        string identifier,
        string name,
        CancellationToken cancellationToken = default);
}
