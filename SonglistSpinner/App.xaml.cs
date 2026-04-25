using SonglistSpinner.Services;

namespace SonglistSpinner;

public partial class App
{
    public App(LocalOverlayServer overlayServer, TwitchAuthService twitchAuth)
    {
        InitializeComponent();
        _ = overlayServer.StartAsync(CancellationToken.None);
        _ = twitchAuth.LoadAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "SonglistSpinner" };
    }
}