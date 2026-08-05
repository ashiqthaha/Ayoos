using Ayoos.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Ayoos.Infrastructure.Identity;

internal sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public string? KeycloakSubject =>
        httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
}
