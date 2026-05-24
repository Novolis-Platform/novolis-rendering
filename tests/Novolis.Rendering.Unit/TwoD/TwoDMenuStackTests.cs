using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDMenuStackTests
{
    [Test]
    public async Task Navigate_WrapsFocus()
    {
        var stack = new TwoDMenuStack();
        stack.Push(new TwoDMenuScreen("TITLE", [
            new TwoDMenuItem("A"),
            new TwoDMenuItem("B"),
        ]));
        stack.Navigate(1);
        await Assert.That(stack.Active!.FocusIndex).IsEqualTo(1);
        stack.Navigate(1);
        await Assert.That(stack.Active!.FocusIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Select_ReturnsTag()
    {
        var stack = new TwoDMenuStack();
        stack.Push(new TwoDMenuScreen("TITLE", [
            new TwoDMenuItem("START", Tag: "play", OnSelect: () => "play"),
        ]));
        await Assert.That(stack.Select()).IsEqualTo("play");
    }
}
