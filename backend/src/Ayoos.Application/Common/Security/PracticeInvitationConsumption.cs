using Ayoos.Application.Common.Exceptions;

namespace Ayoos.Application.Common.Security;

public static class PracticeInvitationConsumption
{
    public static void EnsureSucceeded(int affectedRows)
    {
        if (affectedRows == 0)
        {
            throw new GoneException(
                "This practice invitation has already been used or is no longer valid.");
        }
    }
}
