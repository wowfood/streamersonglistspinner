using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ServerSpinner.Client.Tests.Components;

public class RedirectToLoginTests : BunitContext
{
    private const string ApiBaseUrl = "https://api.example.com";

    public RedirectToLoginTests()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = ApiBaseUrl })
            .Build());
    }

    [Fact]
    public void Given_RedirectToLogin_When_Rendered_Then_NavigatesToTwitchLoginUrl()
    {
        Render<RedirectToLogin>();

        var navManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        Assert.Equal($"{ApiBaseUrl}/api/auth/twitch/login", navManager.Uri);
    }

    [Fact]
    public void Given_RedirectToLogin_When_Rendered_Then_NavigatesWithForceLoad()
    {
        Render<RedirectToLogin>();

        var navManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        var lastNavigation = navManager.History.Last();
        Assert.True(lastNavigation.Options.ForceLoad);
    }
}
