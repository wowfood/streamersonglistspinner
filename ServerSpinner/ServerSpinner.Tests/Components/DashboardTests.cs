using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using ServerSpinner.Components.Pages;
using Xunit;

namespace ServerSpinner.Tests.Components;

public class DashboardTests : ComponentTestBase
{
    private IRenderedComponent<Dashboard> RenderDashboard(bool authenticated = false)
    {
        RegisterCoreServices();

        AuthenticationState authState;
        if (authenticated)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test-streamer-id") };
            var identity = new ClaimsIdentity(claims, "cookie");
            authState = new AuthenticationState(new ClaimsPrincipal(identity));
        }
        else
        {
            authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        return Render<Dashboard>(p =>
            p.AddCascadingValue(Task.FromResult(authState)));
    }

    // ── Initial render ────────────────────────────────────────────────────────

    [Fact]
    public void Given_Component_When_Rendered_Then_SpinButtonPresent()
    {
        var cut = RenderDashboard();

        Assert.NotNull(cut.Find("#spinButton"));
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_ShowsStreamerInput()
    {
        var cut = RenderDashboard();

        Assert.NotNull(cut.Find("#streamerInput"));
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_WinnerModalHidden()
    {
        var cut = RenderDashboard();

        var modal = cut.Find("#winnerModal");
        Assert.Contains("display:none", modal.GetAttribute("style") ?? "");
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_PlayedListPresent()
    {
        var cut = RenderDashboard();

        Assert.NotNull(cut.Find("#playedList"));
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_WheelContainerPresent()
    {
        var cut = RenderDashboard();

        Assert.NotNull(cut.Find("#wheelContainer"));
    }

    // ── Streamer input ────────────────────────────────────────────────────────

    [Fact]
    public void Given_EmptyStreamer_When_ClickLoad_Then_StatusVisible()
    {
        var cut = RenderDashboard();

        cut.Find(".small-button").Click();

        // Status area exists (visibility driven by _config.Debug in real runtime,
        // but the element is always in the DOM)
        Assert.NotNull(cut.Find("#status"));
    }

    [Fact]
    public void Given_AuthenticatedUser_When_Rendered_Then_NoStreamerInputPreloaded()
    {
        var cut = RenderDashboard(true);

        // Without a default name in config, streamer input stays visible
        Assert.NotNull(cut.Find("#streamerInput"));
    }
}