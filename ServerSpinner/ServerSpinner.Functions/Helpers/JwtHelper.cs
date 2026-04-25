using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ServerSpinner.Functions.Helpers;

public static class JwtHelper
{
    private const string Issuer = "ServerSpinner";
    private const string Audience = "ServerSpinner";

    public static string Create(string streamerId, string displayName, string secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, streamerId),
            new Claim(JwtRegisteredClaimNames.Name, displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ClaimsPrincipal? Validate(string? token, string secret)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public static string? ExtractFromCookies(string? cookieHeader)
    {
        if (string.IsNullOrEmpty(cookieHeader)) return null;

        foreach (var part in cookieHeader.Split(';'))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("ss_auth=", StringComparison.OrdinalIgnoreCase))
                return trimmed["ss_auth=".Length..];
        }

        return null;
    }

    public static (string? StreamerId, string? DisplayName) GetClaims(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = principal.FindFirstValue(JwtRegisteredClaimNames.Name)
                   ?? principal.FindFirstValue(ClaimTypes.Name);
        return (id, name);
    }
}