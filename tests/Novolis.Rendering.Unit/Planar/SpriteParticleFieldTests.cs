using Novolis.Rendering.Planar;

namespace Novolis.Rendering.Unit.Planar;

public sealed class SpriteParticleFieldTests
{
    [Test]
    public async Task Full_pool_replacement_is_repeatable_for_the_same_seed()
    {
        static SpriteParticle Particle(int kind) =>
            new()
            {
                Kind = kind,
                MaxLife = 10,
                Life = 10,
            };

        var first = new SpriteParticleField(2, seed: 1234);
        var second = new SpriteParticleField(2, seed: 1234);
        for (var kind = 1; kind <= 5; kind++)
        {
            var particle = Particle(kind);
            first.Emit(particle);
            second.Emit(particle);
        }

        await Assert.That(first.Count).IsEqualTo(second.Count);
        await Assert.That(first.Span.ToArray())
            .IsEquivalentTo(second.Span.ToArray());
    }
}
