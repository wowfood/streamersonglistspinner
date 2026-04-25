using MudBlazor.Utilities;
using ServerSpinner.Core.Data;
using SonglistSpinner.Extensions;

namespace SonglistSpinner.Components.Pages;

// Injected properties (LocalSettings, Config) come from @inject in Settings.razor.
public partial class Settings
{
    private readonly SettingsViewModel _vm = new();
    private string _apiBaseUrl = "";
    private SettingsDto? _dto;

    private MudColor ColorBackground
    {
        get => (_dto?.BackgroundColor ?? "#000000").ToMudColor();
        set
        {
            if (_dto != null) _dto.BackgroundColor = value.ToHexString();
        }
    }

    private MudColor ColorText
    {
        get => (_dto?.ColorText ?? "#000000").ToMudColor();
        set
        {
            if (_dto != null) _dto.ColorText = value.ToHexString();
        }
    }

    private MudColor ColorPointer
    {
        get => (_dto?.ColorPointer ?? "#000000").ToMudColor();
        set
        {
            if (_dto != null) _dto.ColorPointer = value.ToHexString();
        }
    }

    private MudColor ColorButtonBg
    {
        get => (_dto?.ColorButtonBackground ?? "#000000").ToMudColor();
        set
        {
            if (_dto != null) _dto.ColorButtonBackground = value.ToHexString();
        }
    }

    private MudColor ColorButtonText
    {
        get => (_dto?.ColorButtonText ?? "#000000").ToMudColor();
        set
        {
            if (_dto != null) _dto.ColorButtonText = value.ToHexString();
        }
    }

    private MudColor ColorPlayedListBg
    {
        get => _vm.PlayedListBgHex.ToMudColorWithAlpha(_vm.PlayedListBgAlpha);
        set
        {
            _vm.PlayedListBgHex = value.ToHexString();
            _vm.PlayedListBgAlpha = value.A / 255.0;
        }
    }

    private MudColor ColorPlayedItemBg
    {
        get => (_dto?.ColorPlayedItemBackground ?? "#000000").ToMudColor();
        set
        {
            if (_dto != null) _dto.ColorPlayedItemBackground = value.ToHexString();
        }
    }

    protected override Task OnInitializedAsync()
    {
        _dto = LocalSettings.LoadSettings();
        _apiBaseUrl = LocalSettings.GetApiBaseUrl();
        _vm.Initialize(_dto);
        return Task.CompletedTask;
    }

    private void Save()
    {
        _vm.SaveSuccess = false;
        _vm.SaveError = null;
        if (_dto == null) return;

        try
        {
            _vm.ApplyToDto(_dto);
            LocalSettings.SaveSettings(_dto);
            LocalSettings.SetApiBaseUrl(_apiBaseUrl);
            Config["ApiBaseUrl"] = _apiBaseUrl;
            _vm.SaveSuccess = true;
        }
        catch (Exception ex)
        {
            _vm.SaveError = ex.Message;
        }
    }
}