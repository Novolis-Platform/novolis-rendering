using Novolis.Rendering.TwoD;

namespace Novolis.Rendering.Unit.TwoD;

public sealed class TwoDMenuStackExtendedTests
{
    [Test]
    public async Task Pop_clear_and_empty_select()
    {
        var stack = new TwoDMenuStack();
        await Assert.That(stack.IsActive).IsFalse();
        await Assert.That(stack.Select()).IsNull();

        stack.Push(new TwoDMenuScreen("A", [new TwoDMenuItem("One")]));
        await Assert.That(stack.IsActive).IsTrue();
        await Assert.That(stack.Pop()).IsTrue();
        await Assert.That(stack.IsActive).IsFalse();

        stack.Push(new TwoDMenuScreen("B", [new TwoDMenuItem("X")]));
        stack.Clear();
        await Assert.That(stack.IsActive).IsFalse();
    }

    [Test]
    public async Task Navigate_noop_on_empty_stack()
    {
        var stack = new TwoDMenuStack();
        stack.Navigate(1);
        await Assert.That(stack.Active).IsNull();
    }

    [Test]
    public async Task Navigate_negative_direction_wraps()
    {
        var stack = new TwoDMenuStack();
        stack.Push(new TwoDMenuScreen("TITLE", [
            new TwoDMenuItem("A"),
            new TwoDMenuItem("B"),
        ]));
        stack.Navigate(-1);
        await Assert.That(stack.Active!.FocusIndex).IsEqualTo(1);
    }
}
