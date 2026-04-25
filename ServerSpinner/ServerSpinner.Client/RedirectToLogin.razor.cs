using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace ServerSpinner.Client;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    protected override void OnInitialized()
    {
        var apiBaseUrl = Configuration["ApiBaseUrl"] ?? NavigationManager.BaseUri;
        NavigationManager.NavigateTo($"{apiBaseUrl}/api/auth/twitch/login", true);
    }
}