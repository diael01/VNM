using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Utils;

public static class HttpContextAccessTokenHelper
{
    public static async Task<string> GetAccessTokenOrThrowAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new UnauthorizedAccessException("Access token is missing.");
        return accessToken;
    }
}
