namespace ServerSpinner.Core.Data;

public record SpinnerConfigBackground(string Mode, string Color, string Image);

public record SpinnerConfigStreamer(string DefaultName, bool HideChangeOptionWhenDefault);

public record SpinnerConfigSongList(
    string[] Fields,
    bool ExcludePlayedSongs,
    string PlayedListPosition,
    string PlayHistoryPeriod);

public record SpinnerConfigPlayedList(string FontFamily, string FontSize, int MaxLines);

public record SpinnerConfigColors(
    string Text,
    string StatusBackground,
    string PlayedListBackground,
    string PlayedItemBackground,
    string ResizeHandleBackground,
    string ResizeHandleHoverBackground,
    string ToggleBackground,
    string ButtonBackground,
    string ButtonText,
    string Pointer);

public record SpinnerConfigResponse(
    bool Debug,
    string[] WheelColors,
    SpinnerConfigBackground Background,
    SpinnerConfigStreamer Streamer,
    SpinnerConfigSongList SongList,
    SpinnerConfigPlayedList PlayedList,
    SpinnerConfigColors Colors);