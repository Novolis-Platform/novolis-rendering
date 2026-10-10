using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarMenuStackTests
{
    [Test]
    public async Task Navigate_WrapsFocus()
    {
        var stack = new PlanarMenuStack();
        stack.Push(new PlanarMenuScreen("TITLE", [
            new PlanarMenuItem("A"),
            new PlanarMenuItem("B"),
        ]));
        stack.Navigate(1);
        await Assert.That(stack.Active!.FocusIndex).IsEqualTo(1);
        stack.Navigate(1);
        await Assert.That(stack.Active!.FocusIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Select_ReturnsTag()
    {
        var stack = new PlanarMenuStack();
        stack.Push(new PlanarMenuScreen("TITLE", [
            new PlanarMenuItem("START", Tag: "play", OnSelect: () => "play"),
        ]));
        await Assert.That(stack.Select()).IsEqualTo("play");
    }
}
