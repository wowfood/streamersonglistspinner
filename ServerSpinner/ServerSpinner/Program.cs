using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ServerSpinner;
using ServerSpinner.Components;
using ServerSpinner.Core.Contracts;
using ServerSpinner.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient(new CookieCredentialHandler { InnerHandler = new HttpClientHandler() })
{
    BaseAddress = new Uri(apiBaseUrl)
});
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();
// External HttpClient has no credential handler — streamersonglist.com returns CORS wildcard
// which the browser rejects if credentials: 'include' is set.
builder.Services.AddScoped<ISpinnerApiService>(sp =>
{
    var authHttp = sp.GetRequiredService<HttpClient>();
    var externalHttp = new HttpClient(); // plain, no CookieCredentialHandler
    return new SpinnerApiService(authHttp, externalHttp);
});
builder.Services.AddScoped<ISpinnerSyncService, SpinnerSyncService>();

await builder.Build().RunAsync();