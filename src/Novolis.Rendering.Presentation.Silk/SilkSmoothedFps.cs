namespace Novolis.Rendering.Presentation.Silk;

/// <summary>Exponential moving average of frames per second for Silk demos.</summary>
public sealed class SilkSmoothedFps
{
    private float _value;
    private bool _initialized;

    /// <summary>EMA blend factor for new samples (0–1).</summary>
    public float Alpha { get; set; } = 0.1f;

    /// <summary>Current smoothed FPS estimate.</summary>
    public float Value => _value;

    /// <summary>Updates the estimate from a frame delta time in seconds.</summary>
    /// <param name="deltaSeconds">Elapsed time for the frame.</param>
    public void Update(float deltaSeconds)
    {
        if (deltaSeconds <= 1e-6f)
        {
            return;
        }

        var instant = 1f / deltaSeconds;
        _value = _initialized ? _value * (1f - Alpha) + instant * Alpha : instant;
        _initialized = true;
    }
}
