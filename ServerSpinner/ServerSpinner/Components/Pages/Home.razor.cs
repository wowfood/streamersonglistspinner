using System.Security.Claims;

namespace ServerSpinner.Components.Pages;

public partial class Home
{
    private string GetOverlayUrl(ClaimsPrincipal user)
    {
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        return $"{Nav.BaseUri}overlay/{id}";
    }
}