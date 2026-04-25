using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using ServerSpinner.Core.Contracts;
using SonglistSpinner.Services;

namespace SonglistSpinner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddSingleton<ILocalSettingsService, PreferencesSettingsService>();

        // Seed ApiBaseUrl from persisted preferences so all components see the correct value.
        var settingsSvc = new PreferencesSettingsService();
        builder.Configuration["ApiBaseUrl"] = settingsSvc.GetApiBaseUrl();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddSingleton<ITokenStore, SecureStorageTokenStore>();
        builder.Services.AddScoped<ISpinnerApiService, HttpApiService>();
        builder.Services.AddScoped<ISpinnerSyncService, NoOpSyncService>();
        builder.Services.AddSingleton<OverlayStateService>();
        builder.Services.AddSingleton<TwitchAuthService>();
        builder.Services.AddSingleton<TwitchChatService>();
        builder.Services.AddSingleton<LocalOverlayServer>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}