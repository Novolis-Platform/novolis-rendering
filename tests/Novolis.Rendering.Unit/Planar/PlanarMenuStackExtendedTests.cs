using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class PlanarMenuStackExtendedTests
{
    [Test]
    public async Task Pop_clear_and_empty_select()
    {
        var stack = new PlanarMenuStack();
        await Assert.That(stack.IsActive).IsFalse();
        await Assert.That(stack.Select()).IsNull();

        stack.Push(new PlanarMenuScreen("A", [new PlanarMenuItem("One")]));
        await Assert.That(stack.IsActive).IsTrue();
        await Assert.That(stack.Pop()).IsTrue();
        await Assert.That(stack.IsActive).IsFalse();

        stack.Push(new PlanarMenuScreen("B", [new PlanarMenuItem("X")]));
        stack.Clear();
        await Assert.That(stack.IsActive).IsFalse();
    }

    [Test]
    public async Task Navigate_noop_on_empty_stack()
    {
        var stack = new PlanarMenuStack();
        stack.Navigate(1);
        await Assert.That(stack.Active).IsNull();
    }

    [Test]
    public async Task Navigate_negative_direction_wraps()
    {
        var stack = new PlanarMenuStack();
        stack.Push(new PlanarMenuScreen("TITLE", [
            new PlanarMenuItem("A"),
            new PlanarMenuItem("B"),
        ]));
        stack.Navigate(-1);
        await Assert.That(stack.Active!.FocusIndex).IsEqualTo(1);
    }
}
