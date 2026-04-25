using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ServerSpinner;

public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly HttpClient _http;

    public CookieAuthStateProvider(HttpClient http)
    {
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/auth/user");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return Anonymous;

            var user = await response.Content.ReadFromJsonAsync<UserInfo>();
            if (user is null) return Anonymous;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.DisplayName)
            };
            var identity = new ClaimsIdentity(claims, "cookie");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    private sealed class UserInfo
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}