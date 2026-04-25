namespace ServerSpinner.Functions.Entities;

public class StreamerSettings
{
    public Guid Id { get; set; }
    public Guid StreamerId { get; set; }

    public string WheelColors { get; set; } =
        """["#ff6b6b","#4ecdc4","#45b7d1","#f9ca24","#6c5ce7","#a29bfe","#fd79a8","#fdcb6e"]""";

    public string BackgroundMode { get; set; } = "color";
    public string BackgroundColor { get; set; } = "#111111";
    public string BackgroundImage { get; set; } = "";
    public string DefaultStreamerName { get; set; } = "";
    public bool HideChangeOptionWhenDefault { get; set; } = true;
    public string SongListFields { get; set; } = """["artist","title"]""";
    public bool ExcludePlayedSongs { get; set; }
    public string PlayedListPosition { get; set; } = "right";
    public string PlayHistoryPeriod { get; set; } = "week";
    public bool AutoPlay { get; set; }
    public bool DebugMode { get; set; }
    public string ColorText { get; set; } = "#ffffff";
    public string ColorStatusBackground { get; set; } = "rgba(0, 0, 0, 0.7)";
    public string ColorPlayedListBackground { get; set; } = "rgba(0, 0, 0, 0.7)";
    public string ColorPlayedItemBackground { get; set; } = "#222222";
    public string ColorResizeHandleBackground { get; set; } = "#333333";
    public string ColorResizeHandleHoverBackground { get; set; } = "#555555";
    public string ColorToggleBackground { get; set; } = "#222222";
    public string ColorButtonBackground { get; set; } = "#555555";
    public string ColorButtonText { get; set; } = "#CCCCCC";
    public string ColorPointer { get; set; } = "wheat";
    public string PlayedListFontFamily { get; set; } = "sans-serif";
    public string PlayedListFontSize { get; set; } = "0.875rem";
    public int PlayedListMaxLines { get; set; } = 2;
    public string FontFamily { get; set; } = "sans-serif";
    public int FontSize { get; set; } = 16;
    public string Theme { get; set; } = "dark";
    public string StreamerSonglistUrl { get; set; } = "";
    public Streamer Streamer { get; set; } = null!;
}
