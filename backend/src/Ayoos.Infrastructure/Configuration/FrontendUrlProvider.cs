using Ayoos.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Ayoos.Infrastructure.Configuration;

internal sealed class FrontendUrlProvider(IConfiguration configuration)
    : IFrontendUrlProvider
{
    public string BuildPracticeSetupUrl(string rawToken)
    {
        var frontendBaseUrl = configuration["FrontendBaseUrl"]
            ?? throw new InvalidOperationException(
                "Configuration value 'FrontendBaseUrl' was not configured.");

        return $"{frontendBaseUrl.TrimEnd('/')}/setup/{Uri.EscapeDataString(rawToken)}";
    }
}
