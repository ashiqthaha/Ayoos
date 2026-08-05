namespace Ayoos.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    string? KeycloakSubject { get; }
}
