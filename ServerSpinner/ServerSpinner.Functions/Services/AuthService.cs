using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ServerSpinner.Functions.Helpers;

namespace ServerSpinner.Functions.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public ClaimsPrincipal? Authenticate(HttpRequestData req)
    {
        var secret = _config["JwtSecret"]!;
        var cookieHeader = req.Headers.TryGetValues("Cookie", out var cookies)
            ? string.Join("; ", cookies)
            : null;
        var token = JwtHelper.ExtractFromCookies(cookieHeader);
        return JwtHelper.Validate(token, secret);
    }
}