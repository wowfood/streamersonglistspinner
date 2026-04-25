using System.Net.Http.Json;
using ServerSpinner.Core.Data;

namespace ServerSpinner.Components.Pages;

public partial class Settings
{
    private readonly SettingsViewModel _vm = new();
    private SettingsDto? _dto;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _dto = await Http.GetFromJsonAsync<SettingsDto>("api/settings");
        }
        catch (Exception ex) { _ = ex; }

        _dto ??= new SettingsDto();
        _vm.Initialize(_dto);
    }

    private async Task Save()
    {
        _vm.SaveSuccess = false;
        _vm.SaveError = null;
        if (_dto == null) return;

        _vm.ApplyToDto(_dto);

        try
        {
            var response = await Http.PostAsJsonAsync("api/settings", _dto);
            if (response.IsSuccessStatusCode)
                _vm.SaveSuccess = true;
            else
                _vm.SaveError = $"Server returned {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            _vm.SaveError = ex.Message;
        }
    }
}