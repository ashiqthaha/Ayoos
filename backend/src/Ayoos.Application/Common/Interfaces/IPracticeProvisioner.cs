using Ayoos.Domain.Practices;

namespace Ayoos.Application.Common.Interfaces;

public interface IPracticeProvisioner
{
    Task ProvisionAsync(
        Practice practice,
        Guid invitationId,
        string practiceAdminKeycloakUserId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default);
}
