using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerSpinner.Components.Pages;
using Xunit;

namespace ServerSpinner.Tests.Components;

public class HomeTests : ComponentTestBase
{
    private void Setup(string apiBaseUrl = "https://example.com")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = apiBaseUrl })
            .Build();
        Services.AddSingleton<IConfiguration>(config);
    }

    // ── Unauthenticated ───────────────────────────────────────────────────────

    [Fact]
    public void Given_UnauthenticatedUser_When_Rendered_Then_ShowsLoginButton()
    {
        Setup();
        RegisterAuthServices(false);

        var cut = Render<Home>();

        Assert.Contains("Login with Twitch", cut.Markup);
    }

    [Fact]
    public void Given_UnauthenticatedUser_When_Rendered_Then_DashboardLinkAbsent()
    {
        Setup();
        RegisterAuthServices(false);

        var cut = Render<Home>();

        Assert.DoesNotContain("Open Dashboard", cut.Markup);
    }

    [Fact]
    public void Given_UnauthenticatedUser_When_Rendered_Then_LoginButtonPointsToTwitch()
    {
        Setup("https://api.example.com");
        RegisterAuthServices(false);

        var cut = Render<Home>();

        Assert.Contains("https://api.example.com/api/auth/twitch/login", cut.Markup);
    }

    // ── Authenticated ─────────────────────────────────────────────────────────

    [Fact]
    public void Given_AuthenticatedUser_When_Rendered_Then_ShowsDashboardLink()
    {
        Setup();
        RegisterAuthServices(true, "testuser");

        var cut = Render<Home>();

        Assert.Contains("Open Dashboard", cut.Markup);
    }

    [Fact]
    public void Given_AuthenticatedUser_When_Rendered_Then_ShowsSettingsLink()
    {
        Setup();
        RegisterAuthServices(true, "testuser");

        var cut = Render<Home>();

        Assert.Contains("Settings", cut.Markup);
    }

    [Fact]
    public void Given_AuthenticatedUser_When_Rendered_Then_LoginButtonAbsent()
    {
        Setup();
        RegisterAuthServices(true, "testuser");

        var cut = Render<Home>();

        Assert.Empty(cut.FindAll(".ss-btn-twitch"));
    }

    [Fact]
    public void Given_AuthenticatedUser_When_Rendered_Then_ShowsOverlayUrlInput()
    {
        Setup();
        RegisterAuthServices(true, "testuser");

        var cut = Render<Home>();

        Assert.NotNull(cut.Find("#overlay-url-input"));
    }

    // ── Static content ────────────────────────────────────────────────────────

    [Fact]
    public void Given_AnyUser_When_Rendered_Then_ShowsProductDescription()
    {
        Setup();
        RegisterAuthServices(false);

        var cut = Render<Home>();

        Assert.Contains("ServerSpinner", cut.Markup);
        Assert.Contains("StreamerSongList", cut.Markup);
    }
}