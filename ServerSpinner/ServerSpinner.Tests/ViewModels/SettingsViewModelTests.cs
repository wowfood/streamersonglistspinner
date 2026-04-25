using ServerSpinner.Components.Pages;
using ServerSpinner.Core.Data;
using Xunit;

namespace ServerSpinner.Tests.ViewModels;

public class SettingsViewModelNormalizeHexColorTests
{
    [Theory]
    [InlineData("wheat", "#f5deb3")]
    [InlineData("white", "#ffffff")]
    [InlineData("black", "#000000")]
    [InlineData("red", "#ff0000")]
    [InlineData("green", "#008000")]
    [InlineData("blue", "#0000ff")]
    [InlineData("yellow", "#ffff00")]
    [InlineData("orange", "#ffa500")]
    [InlineData("purple", "#800080")]
    [InlineData("pink", "#ffc0cb")]
    [InlineData("gray", "#808080")]
    [InlineData("grey", "#808080")]
    public void Given_NamedColor_When_NormalizeHexColor_Then_ReturnsHex(string input, string expected)
    {
        var result = SettingsViewModel.NormalizeHexColor(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Given_HexColor_When_NormalizeHexColor_Then_ReturnsUnchanged()
    {
        var result = SettingsViewModel.NormalizeHexColor("#aabbcc");

        Assert.Equal("#aabbcc", result);
    }

    [Fact]
    public void Given_UnknownName_When_NormalizeHexColor_Then_ReturnsFallback()
    {
        var result = SettingsViewModel.NormalizeHexColor("chartreuse");

        Assert.Equal("#f5deb3", result);
    }

    [Fact]
    public void Given_UpperCaseNamedColor_When_NormalizeHexColor_Then_ReturnsHex()
    {
        var result = SettingsViewModel.NormalizeHexColor("WHITE");

        Assert.Equal("#ffffff", result);
    }
}

public class SettingsViewModelInitPlayedListBgTests
{
    [Fact]
    public void Given_ValidRgba_When_InitPlayedListBg_Then_ParsesHexAndAlpha()
    {
        var vm = new SettingsViewModel();

        vm.InitPlayedListBg("rgba(0,0,0,0.7)");

        Assert.Equal("#000000", vm.PlayedListBgHex);
        Assert.Equal(0.7, vm.PlayedListBgAlpha, 5);
    }

    [Fact]
    public void Given_RgbaWithSpaces_When_InitPlayedListBg_Then_Parses()
    {
        var vm = new SettingsViewModel();

        vm.InitPlayedListBg("rgba(255, 128, 0, 0.5)");

        Assert.Equal("#FF8000", vm.PlayedListBgHex);
        Assert.Equal(0.5, vm.PlayedListBgAlpha, 5);
    }

    [Fact]
    public void Given_HexValue_When_InitPlayedListBg_Then_SetsHexAndAlphaOne()
    {
        var vm = new SettingsViewModel();

        vm.InitPlayedListBg("#112233");

        Assert.Equal("#112233", vm.PlayedListBgHex);
        Assert.Equal(1.0, vm.PlayedListBgAlpha, 5);
    }

    [Fact]
    public void Given_UnknownValue_When_InitPlayedListBg_Then_FallsBackToBlack()
    {
        var vm = new SettingsViewModel();

        vm.InitPlayedListBg("transparent");

        Assert.Equal("#000000", vm.PlayedListBgHex);
        Assert.Equal(1.0, vm.PlayedListBgAlpha, 5);
    }

    [Fact]
    public void Given_MalformedRgba_When_InitPlayedListBg_Then_FallsBackToHexBehavior()
    {
        var vm = new SettingsViewModel();

        vm.InitPlayedListBg("rgba(bad,data)");

        Assert.Equal("#000000", vm.PlayedListBgHex);
        Assert.Equal(1.0, vm.PlayedListBgAlpha, 5);
    }
}

public class SettingsViewModelComputedPlayedListBgTests
{
    [Fact]
    public void Given_ValidHexAndAlpha_When_ComputedPlayedListBg_Then_ReturnsRgba()
    {
        var vm = new SettingsViewModel { PlayedListBgHex = "#000000", PlayedListBgAlpha = 0.7 };

        var result = vm.ComputedPlayedListBg();

        Assert.Equal("rgba(0,0,0,0.70)", result);
    }

    [Fact]
    public void Given_ColorHex_When_ComputedPlayedListBg_Then_ExtractsRgbComponents()
    {
        var vm = new SettingsViewModel { PlayedListBgHex = "#ff8000", PlayedListBgAlpha = 1.0 };

        var result = vm.ComputedPlayedListBg();

        Assert.Equal("rgba(255,128,0,1.00)", result);
    }

    [Fact]
    public void Given_InvalidHex_When_ComputedPlayedListBg_Then_ReturnsFallback()
    {
        var vm = new SettingsViewModel { PlayedListBgHex = "notahex", PlayedListBgAlpha = 0.5 };

        var result = vm.ComputedPlayedListBg();

        Assert.Equal("notahex", result);
    }
}

public class SettingsViewModelInitDisplayFieldsTests
{
    [Fact]
    public void Given_ValidJson_When_InitDisplayFields_Then_SelectedFieldsFirst()
    {
        var vm = new SettingsViewModel();

        vm.InitDisplayFields("""["artist","title"]""");

        Assert.Equal(["artist", "title", "requester", "donation"],
            vm.DisplayFields.Select(f => f.Name));
    }

    [Fact]
    public void Given_ValidJson_When_InitDisplayFields_Then_SelectedFieldsAreMarked()
    {
        var vm = new SettingsViewModel();

        vm.InitDisplayFields("""["artist","title"]""");

        Assert.True(vm.DisplayFields[0].Selected);
        Assert.True(vm.DisplayFields[1].Selected);
        Assert.False(vm.DisplayFields[2].Selected);
        Assert.False(vm.DisplayFields[3].Selected);
    }

    [Fact]
    public void Given_AllFieldsSelected_When_InitDisplayFields_Then_AllMarkedSelected()
    {
        var vm = new SettingsViewModel();

        vm.InitDisplayFields("""["artist","title","requester","donation"]""");

        Assert.All(vm.DisplayFields, f => Assert.True(f.Selected));
    }

    [Fact]
    public void Given_InvalidJson_When_InitDisplayFields_Then_FallsBackToArtistTitle()
    {
        var vm = new SettingsViewModel();

        vm.InitDisplayFields("not valid json");

        var selected = vm.DisplayFields.Where(f => f.Selected).Select(f => f.Name).ToList();
        Assert.Equal(["artist", "title"], selected);
    }

    [Fact]
    public void Given_EmptyArray_When_InitDisplayFields_Then_NoSelectedFields()
    {
        var vm = new SettingsViewModel();

        vm.InitDisplayFields("[]");

        Assert.Equal(4, vm.DisplayFields.Count);
        Assert.All(vm.DisplayFields, f => Assert.False(f.Selected));
    }

    [Fact]
    public void Given_UnknownFieldName_When_InitDisplayFields_Then_Ignored()
    {
        var vm = new SettingsViewModel();

        vm.InitDisplayFields("""["artist","unknownfield"]""");

        Assert.Equal(4, vm.DisplayFields.Count);
        Assert.DoesNotContain(vm.DisplayFields, f => f.Name == "unknownfield");
    }
}

public class SettingsViewModelToggleFieldTests
{
    [Fact]
    public void Given_SelectedField_When_ToggleField_Then_BecomesUnselected()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title"]""");

        vm.ToggleField(0);

        Assert.False(vm.DisplayFields[0].Selected);
    }

    [Fact]
    public void Given_UnselectedField_When_ToggleField_Then_BecomesSelected()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title"]""");

        vm.ToggleField(2);

        Assert.True(vm.DisplayFields[2].Selected);
    }
}

public class SettingsViewModelDropFieldTests
{
    [Fact]
    public void Given_DragIdxNegative_When_DropField_Then_NoChange()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title","requester","donation"]""");
        vm.DragIdx = -1;
        var originalOrder = vm.DisplayFields.Select(f => f.Name).ToList();

        vm.DropField(2);

        Assert.Equal(originalOrder, vm.DisplayFields.Select(f => f.Name));
    }

    [Fact]
    public void Given_SameSourceAndTarget_When_DropField_Then_NoChange()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title","requester","donation"]""");
        vm.DragIdx = 1;
        var originalOrder = vm.DisplayFields.Select(f => f.Name).ToList();

        vm.DropField(1);

        Assert.Equal(originalOrder, vm.DisplayFields.Select(f => f.Name));
    }

    [Fact]
    public void Given_DragForward_When_DropField_Then_ReordersCorrectly()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title","requester","donation"]""");
        vm.DragIdx = 0;

        vm.DropField(2);

        Assert.Equal(["title", "artist", "requester", "donation"],
            vm.DisplayFields.Select(f => f.Name));
    }

    [Fact]
    public void Given_DragBackward_When_DropField_Then_ReordersCorrectly()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title","requester","donation"]""");
        vm.DragIdx = 3;

        vm.DropField(1);

        Assert.Equal(["artist", "donation", "title", "requester"],
            vm.DisplayFields.Select(f => f.Name));
    }

    [Fact]
    public void Given_SuccessfulDrop_When_DropField_Then_ResetsDragState()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title","requester","donation"]""");
        vm.DragIdx = 0;
        vm.DragOverIdx = 2;

        vm.DropField(2);

        Assert.Equal(-1, vm.DragIdx);
        Assert.Equal(-1, vm.DragOverIdx);
    }
}

public class SettingsViewModelApplyToDtoTests
{
    [Fact]
    public void Given_WheelColorsRaw_When_ApplyToDto_Then_SerializesColorsJson()
    {
        var vm = new SettingsViewModel { WheelColorsRaw = "#ff0000\n#00ff00" };
        vm.InitDisplayFields("""["artist","title"]""");
        vm.InitPlayedListBg("#000000");
        var dto = new SettingsDto();

        vm.ApplyToDto(dto);

        Assert.Contains("ff0000", dto.WheelColors);
        Assert.Contains("00ff00", dto.WheelColors);
    }

    [Fact]
    public void Given_SelectedFields_When_ApplyToDto_Then_SerializesFieldsJson()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("""["artist","title"]""");
        vm.InitPlayedListBg("#000000");
        var dto = new SettingsDto();

        vm.ApplyToDto(dto);

        Assert.Contains("artist", dto.SongListFields);
        Assert.Contains("title", dto.SongListFields);
    }

    [Fact]
    public void Given_NoSelectedFields_When_ApplyToDto_Then_FallsBackToArtistTitle()
    {
        var vm = new SettingsViewModel();
        vm.InitDisplayFields("[]");
        vm.InitPlayedListBg("#000000");
        var dto = new SettingsDto();

        vm.ApplyToDto(dto);

        Assert.Contains("artist", dto.SongListFields);
        Assert.Contains("title", dto.SongListFields);
    }

    [Fact]
    public void Given_BackgroundState_When_ApplyToDto_Then_SetsComputedRgba()
    {
        var vm = new SettingsViewModel { PlayedListBgHex = "#000000", PlayedListBgAlpha = 0.5 };
        vm.InitDisplayFields("""["artist"]""");
        var dto = new SettingsDto();

        vm.ApplyToDto(dto);

        Assert.StartsWith("rgba(", dto.ColorPlayedListBackground);
    }
}