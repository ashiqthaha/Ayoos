using Ayoos.Domain.Practices;

namespace Ayoos.Application.Common.Interfaces;

public interface IPracticeProvisioner
{
    Task ProvisionAsync(Practice practice, CancellationToken cancellationToken = default);
}
