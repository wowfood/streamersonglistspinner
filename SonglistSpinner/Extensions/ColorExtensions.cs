using MudBlazor.Utilities;

namespace SonglistSpinner.Extensions;

public static class ColorExtensions
{
    public static MudColor ToMudColor(this string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return new MudColor("#000000");
        try
        {
            return new MudColor(NormalizeHex(color));
        }
        catch
        {
            return new MudColor("#000000");
        }
    }

    public static MudColor ToMudColorWithAlpha(this string? hex, double alpha)
    {
        var c = hex.ToMudColor();
        return new MudColor(c.R, c.G, c.B, (byte)(alpha * 255));
    }

    public static string ToHexString(this MudColor color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static string NormalizeHex(string color)
    {
        return color.ToLower() switch
        {
            "wheat" => "#f5deb3",
            "white" => "#ffffff",
            "black" => "#000000",
            "red" => "#ff0000",
            "green" => "#008000",
            "blue" => "#0000ff",
            "yellow" => "#ffff00",
            "orange" => "#ffa500",
            "purple" => "#800080",
            "pink" => "#ffc0cb",
            "gray" or "grey" => "#808080",
            _ => color.StartsWith('#') ? color : "#000000"
        };
    }
}