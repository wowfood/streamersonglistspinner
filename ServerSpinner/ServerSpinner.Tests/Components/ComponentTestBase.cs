using System.Net;
using System.Security.Claims;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using ServerSpinner.Core.Contracts;
using ServerSpinner.Services;

namespace ServerSpinner.Tests.Components;

public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected void RegisterCoreServices(string apiBaseUrl = "https://example.com")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ApiBaseUrl"] = apiBaseUrl })
            .Build();
        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<ISpinnerApiService>(CreateApiService());
        Services.AddSingleton<ISpinnerSyncService>(CreateSyncService());
    }

    protected void RegisterAuthServices(bool authenticated = false, string? username = null)
    {
        foreach (var d in Services.Where(d => d.ServiceType == typeof(IAuthorizationService)).ToList())
            Services.Remove(d);
        Services.AddAuthorizationCore();

        foreach (var d in Services.Where(d => d.ServiceType == typeof(AuthenticationStateProvider)).ToList())
            Services.Remove(d);
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(authenticated, username));
    }

    protected SpinnerApiService CreateApiService(string content = "{}")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://example.com") };
        return new SpinnerApiService(http, http);
    }

    protected SpinnerSyncService CreateSyncService()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        return new SpinnerSyncService(new HttpClient(handler.Object));
    }

    protected HttpClient CreateHttpClient(HttpStatusCode status, string content = "null")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        return new HttpClient(handler.Object) { BaseAddress = new Uri("https://example.com") };
    }
}

internal sealed class FakeAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationState _state;

    internal FakeAuthStateProvider(bool authenticated, string? username = null)
    {
        if (authenticated)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, username ?? "testuser") };
            var identity = new ClaimsIdentity(claims, "test");
            _state = new AuthenticationState(new ClaimsPrincipal(identity));
        }
        else
        {
            _state = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(_state);
    }
}