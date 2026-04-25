using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ServerSpinner.Components.Pages;
using ServerSpinner.Core.Data;
using Xunit;

namespace ServerSpinner.Tests.Components;

public class SettingsComponentTests : ComponentTestBase
{
    private IRenderedComponent<Settings> RenderSettings(
        HttpStatusCode getStatus = HttpStatusCode.OK,
        SettingsDto? model = null)
    {
        var content = model != null
            ? JsonSerializer.Serialize(model)
            : JsonSerializer.Serialize(new SettingsDto());

        Services.AddSingleton(CreateHttpClient(getStatus, content));
        return Render<Settings>();
    }

    // ── Loading state ─────────────────────────────────────────────────────────

    [Fact]
    public void Given_SuccessfulLoad_When_Rendered_Then_ShowsEditForm()
    {
        var cut = RenderSettings();

        Assert.NotNull(cut.Find("form"));
    }

    [Fact]
    public void Given_SuccessfulLoad_When_Rendered_Then_ShowsSaveButton()
    {
        var cut = RenderSettings();

        var btn = cut.Find("button[type='submit']");
        Assert.NotNull(btn);
    }

    [Fact]
    public void Given_SuccessfulLoad_When_Rendered_Then_ShowsDisplayFieldChips()
    {
        var cut = RenderSettings();

        var chips = cut.FindAll(".ss-chip");
        Assert.NotEmpty(chips);
    }

    [Fact]
    public void Given_SuccessfulLoad_When_Rendered_Then_AllFourFieldChipsPresent()
    {
        var cut = RenderSettings();

        var chips = cut.FindAll(".ss-chip");
        Assert.Equal(4, chips.Count);
    }

    // ── ViewModel integration ─────────────────────────────────────────────────

    [Fact]
    public void Given_ModelWithSelectedFields_When_Rendered_Then_SelectedChipsHaveCorrectClass()
    {
        var model = new SettingsDto { SongListFields = """["artist","title"]""" };

        var cut = RenderSettings(model: model);

        var selected = cut.FindAll(".ss-chip-selected");
        Assert.Equal(2, selected.Count);
    }

    [Fact]
    public void Given_FieldChip_When_Clicked_Then_TogglesSelectedClass()
    {
        var cut = RenderSettings();
        var chipsBefore = cut.FindAll(".ss-chip-selected").Count;

        cut.FindAll(".ss-chip")[0].Click();

        var chipsAfter = cut.FindAll(".ss-chip-selected").Count;
        Assert.NotEqual(chipsBefore, chipsAfter);
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public void Given_FailedLoad_When_Rendered_Then_StillShowsForm()
    {
        // Component catches the exception and initialises with defaults
        var cut = RenderSettings(HttpStatusCode.InternalServerError);

        Assert.NotNull(cut.Find("form"));
    }
}