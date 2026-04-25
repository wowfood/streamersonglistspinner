using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServerSpinner.Functions.Helpers;
using Xunit;

namespace ServerSpinner.Functions.Tests.Helpers;

public class JwtHelperTests
{
    private const string ValidSecret = "this-is-a-test-secret-that-is-long-enough-for-hmac-sha256";
    private const string StreamerId = "550e8400-e29b-41d4-a716-446655440000";
    private const string DisplayName = "TestStreamer";

    // ── Create / round-trip ──────────────────────────────────────────────────

    [Fact]
    public void Given_ValidInputs_When_Create_Then_ReturnsTokenWithThreeSegments()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);

        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void Given_CreatedToken_When_Validate_Then_ReturnsPrincipal()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);

        var principal = JwtHelper.Validate(token, ValidSecret);

        Assert.NotNull(principal);
    }

    [Fact]
    public void Given_CreatedToken_When_GetClaims_Then_ReturnsOriginalStreamerId()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);
        var principal = JwtHelper.Validate(token, ValidSecret)!;

        var (id, _) = JwtHelper.GetClaims(principal);

        Assert.Equal(StreamerId, id);
    }

    [Fact]
    public void Given_CreatedToken_When_GetClaims_Then_ReturnsOriginalDisplayName()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);
        var principal = JwtHelper.Validate(token, ValidSecret)!;

        var (_, name) = JwtHelper.GetClaims(principal);

        Assert.Equal(DisplayName, name);
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Given_NullToken_When_Validate_Then_ReturnsNull()
    {
        var principal = JwtHelper.Validate(null, ValidSecret);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_EmptyToken_When_Validate_Then_ReturnsNull()
    {
        var principal = JwtHelper.Validate("", ValidSecret);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_WhitespaceToken_When_Validate_Then_ReturnsNull()
    {
        var principal = JwtHelper.Validate("   ", ValidSecret);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_MalformedToken_When_Validate_Then_ReturnsNull()
    {
        var principal = JwtHelper.Validate("not.a.valid.token", ValidSecret);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_TokenSignedWithWrongSecret_When_Validate_Then_ReturnsNull()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);

        var principal = JwtHelper.Validate(token, "a-completely-different-secret-value-here-1234");

        Assert.Null(principal);
    }

    [Fact]
    public void Given_ExpiredToken_When_Validate_Then_ReturnsNull()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidSecret));
        var expiredToken = new JwtSecurityToken(
            "ServerSpinner",
            "ServerSpinner",
            [new Claim(JwtRegisteredClaimNames.Sub, StreamerId)],
            expires: DateTime.UtcNow.AddDays(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        var principal = JwtHelper.Validate(tokenString, ValidSecret);

        Assert.Null(principal);
    }

    // ── ExtractFromCookies ────────────────────────────────────────────────────

    [Fact]
    public void Given_CookieHeaderWithOnlyAuthCookie_When_ExtractFromCookies_Then_ReturnsToken()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);

        var extracted = JwtHelper.ExtractFromCookies($"ss_auth={token}");

        Assert.Equal(token, extracted);
    }

    [Fact]
    public void Given_CookieHeaderWithMultipleCookies_When_ExtractFromCookies_Then_ReturnsAuthToken()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);

        var extracted = JwtHelper.ExtractFromCookies($"session=abc; ss_auth={token}; theme=dark");

        Assert.Equal(token, extracted);
    }

    [Fact]
    public void Given_CookieHeaderWithoutAuthCookie_When_ExtractFromCookies_Then_ReturnsNull()
    {
        var extracted = JwtHelper.ExtractFromCookies("session=abc123; theme=dark");

        Assert.Null(extracted);
    }

    [Fact]
    public void Given_NullCookieHeader_When_ExtractFromCookies_Then_ReturnsNull()
    {
        var extracted = JwtHelper.ExtractFromCookies(null);

        Assert.Null(extracted);
    }

    [Fact]
    public void Given_EmptyCookieHeader_When_ExtractFromCookies_Then_ReturnsNull()
    {
        var extracted = JwtHelper.ExtractFromCookies("");

        Assert.Null(extracted);
    }

    [Fact]
    public void Given_AuthCookieNameInUppercase_When_ExtractFromCookies_Then_ReturnsToken()
    {
        var token = JwtHelper.Create(StreamerId, DisplayName, ValidSecret);

        var extracted = JwtHelper.ExtractFromCookies($"SS_AUTH={token}");

        Assert.Equal(token, extracted);
    }

    // ── GetClaims ─────────────────────────────────────────────────────────────

    [Fact]
    public void Given_PrincipalWithSubAndNameClaims_When_GetClaims_Then_ReturnsBothValues()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, StreamerId),
            new Claim(JwtRegisteredClaimNames.Name, DisplayName)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var (id, name) = JwtHelper.GetClaims(principal);

        Assert.Equal(StreamerId, id);
        Assert.Equal(DisplayName, name);
    }

    [Fact]
    public void Given_PrincipalWithNoClaims_When_GetClaims_Then_ReturnsNullValues()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>()));

        var (id, name) = JwtHelper.GetClaims(principal);

        Assert.Null(id);
        Assert.Null(name);
    }

    [Fact]
    public void Given_PrincipalWithNameIdentifierClaim_When_GetClaims_Then_ReturnsStreamerId()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, StreamerId),
            new Claim(ClaimTypes.Name, DisplayName)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var (id, _) = JwtHelper.GetClaims(principal);

        Assert.Equal(StreamerId, id);
    }
}