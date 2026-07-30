namespace Novolis.Rendering.Presentation;

/// <summary>
/// Keyboard keys for Silk/OpenGL hosts. Numeric values match Silk.NET.Input.Key
/// so backends can cast without a lookup table.
/// </summary>
public enum Key
{
    /// <summary>Space bar.</summary>
    Space = 32,

    /// <summary>Minus / hyphen.</summary>
    Minus = 45,

    /// <summary>Equals.</summary>
    Equal = 61,

    /// <summary>Digit 1.</summary>
    Number1 = 49,

    /// <summary>Digit 2.</summary>
    Number2 = 50,

    /// <summary>Digit 3.</summary>
    Number3 = 51,

    /// <summary>Digit 4.</summary>
    Number4 = 52,

    /// <summary>Digit 5.</summary>
    Number5 = 53,

    /// <summary>A key.</summary>
    A = 65,

    /// <summary>B key.</summary>
    B = 66,

    /// <summary>D key.</summary>
    D = 68,

    /// <summary>E key.</summary>
    E = 69,

    /// <summary>H key.</summary>
    H = 72,

    /// <summary>J key.</summary>
    J = 74,

    /// <summary>R key.</summary>
    R = 82,

    /// <summary>S key.</summary>
    S = 83,

    /// <summary>W key.</summary>
    W = 87,

    /// <summary>Unknown / unsupported.</summary>
    Unknown = -1,

    /// <summary>Escape.</summary>
    Escape = 256,

    /// <summary>Enter / return.</summary>
    Enter = 257,

    /// <summary>Arrow down.</summary>
    Down = 264,

    /// <summary>Arrow up.</summary>
    Up = 265,

    /// <summary>Keypad subtract.</summary>
    KeypadSubtract = 333,

    /// <summary>Keypad add.</summary>
    KeypadAdd = 334,

    /// <summary>Left shift.</summary>
    ShiftLeft = 340,

    /// <summary>Right shift.</summary>
    ShiftRight = 344,
}
