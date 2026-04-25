using System.Globalization;
using System.Text.Json;
using ServerSpinner.Core.Data;

namespace SonglistSpinner.Components.Pages;

public sealed class SettingsViewModel
{
    private static readonly string[] ValidFields = ["artist", "title", "requester", "donation"];

    public string WheelColorsRaw { get; set; } = "";
    public bool SaveSuccess { get; set; }
    public string? SaveError { get; set; }
    public List<DisplayField> DisplayFields { get; private set; } = new();
    public int DragIdx { get; set; } = -1;
    public int DragOverIdx { get; set; } = -1;
    public string PlayedListBgHex { get; set; } = "#000000";
    public double PlayedListBgAlpha { get; set; } = 0.7;

    public void Initialize(SettingsDto dto)
    {
        try
        {
            WheelColorsRaw = string.Join("\n", JsonSerializer.Deserialize<string[]>(dto.WheelColors) ?? []);
        }
        catch
        {
            WheelColorsRaw = "#ff6b6b\n#4ecdc4\n#45b7d1\n#f9ca24\n#6c5ce7\n#a29bfe\n#fd79a8\n#fdcb6e";
        }

        InitDisplayFields(dto.SongListFields);
        InitPlayedListBg(dto.ColorPlayedListBackground);
        dto.ColorPointer = NormalizeHexColor(dto.ColorPointer);
    }

    public void InitDisplayFields(string json)
    {
        string[] selected;
        try
        {
            selected = JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch
        {
            selected = ["artist", "title"];
        }

        DisplayFields = selected
            .Where(f => ValidFields.Contains(f))
            .Select(f => new DisplayField { Name = f, Selected = true })
            .Concat(ValidFields.Except(selected).Select(f => new DisplayField { Name = f, Selected = false }))
            .ToList();
    }

    public void ToggleField(int idx)
    {
        DisplayFields[idx].Selected = !DisplayFields[idx].Selected;
    }

    public void DropField(int targetIdx)
    {
        if (DragIdx < 0 || DragIdx == targetIdx) return;
        var item = DisplayFields[DragIdx];
        DisplayFields.RemoveAt(DragIdx);
        var insertIdx = DragIdx < targetIdx ? targetIdx - 1 : targetIdx;
        DisplayFields.Insert(insertIdx, item);
        DragIdx = -1;
        DragOverIdx = -1;
    }

    public void InitPlayedListBg(string value)
    {
        if (value.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase))
        {
            var parts = value[5..^1].Split(',');
            if (parts.Length == 4 &&
                int.TryParse(parts[0].Trim(), out var r) &&
                int.TryParse(parts[1].Trim(), out var g) &&
                int.TryParse(parts[2].Trim(), out var b) &&
                double.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
            {
                PlayedListBgHex = $"#{r:X2}{g:X2}{b:X2}";
                PlayedListBgAlpha = a;
                return;
            }
        }

        PlayedListBgHex = value.StartsWith('#') ? value[..Math.Min(7, value.Length)] : "#000000";
        PlayedListBgAlpha = 1.0;
    }

    public string ComputedPlayedListBg()
    {
        var hex = PlayedListBgHex.TrimStart('#');
        if (hex.Length >= 6 &&
            int.TryParse(hex[..2], NumberStyles.HexNumber, null, out var r) &&
            int.TryParse(hex[2..4], NumberStyles.HexNumber, null, out var g) &&
            int.TryParse(hex[4..6], NumberStyles.HexNumber, null, out var b))
            return $"rgba({r},{g},{b},{PlayedListBgAlpha.ToString("F2", CultureInfo.InvariantCulture)})";
        return PlayedListBgHex;
    }

    public static string NormalizeHexColor(string color)
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
            _ => color.StartsWith('#') ? color : "#f5deb3"
        };
    }

    public void ApplyToDto(SettingsDto dto)
    {
        var colors = WheelColorsRaw
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToArray();
        dto.WheelColors = JsonSerializer.Serialize(colors);

        var fields = DisplayFields.Where(f => f.Selected).Select(f => f.Name).ToArray();
        dto.SongListFields = JsonSerializer.Serialize(fields.Length > 0 ? fields : new[] { "artist", "title" });

        dto.ColorPlayedListBackground = ComputedPlayedListBg();
    }
}

public sealed class DisplayField
{
    public string Name { get; set; } = "";
    public bool Selected { get; set; }
}