using Bunit;
using ServerSpinner.Components.Pages;
using Xunit;

namespace ServerSpinner.Tests.Components;

public class OverlayTests : ComponentTestBase
{
    private static readonly Guid TestStreamerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private IRenderedComponent<Overlay> RenderOverlay(Guid? streamerId = null)
    {
        RegisterCoreServices();
        return Render<Overlay>(p =>
            p.Add(o => o.StreamerId, streamerId ?? TestStreamerId));
    }

    // ── Initial render ────────────────────────────────────────────────────────

    [Fact]
    public void Given_Component_When_Rendered_Then_WheelSectionPresent()
    {
        var cut = RenderOverlay();

        Assert.NotNull(cut.Find("#wheelSection"));
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_PlayedListPresent()
    {
        var cut = RenderOverlay();

        Assert.NotNull(cut.Find("#playedList"));
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_WinnerModalHidden()
    {
        var cut = RenderOverlay();

        var modal = cut.Find("#winnerModal");
        Assert.Contains("display:none", modal.GetAttribute("style") ?? "");
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_WheelContainerPresent()
    {
        var cut = RenderOverlay();

        Assert.NotNull(cut.Find("#wheelContainer"));
    }

    [Fact]
    public void Given_Component_When_Rendered_Then_ResizeHandleHidden()
    {
        var cut = RenderOverlay();

        var handle = cut.Find("#resizeHandle");
        Assert.Contains("display:none", handle.GetAttribute("style") ?? "");
    }

    // ── StreamerId parameter ───────────────────────────────────────────────────

    [Fact]
    public void Given_StreamerId_When_Rendered_Then_ComponentRendersWithoutError()
    {
        var id = Guid.NewGuid();

        var cut = RenderOverlay(id);

        Assert.NotNull(cut.Instance);
    }
}