/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Author : TakoPuck (2026)                                                        *
 * Licence: You are free to use, modify, and distribute this code for any purpose. *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using Microsoft.Xna.Framework;

namespace CoolCustomText.Source;

public readonly record struct TextInfo
{   
    public string Text { get; init; } = string.Empty;

    public Vector2 Position { get; init; } = Vector2.Zero;

    public Vector2 Dimension { get; init; } = Vector2.Zero;

    public Vector2 Scale { get; init; } = Vector2.One;

    public Vector2 Padding { get; init; } = Vector2.Zero;

    public Color Color { get; init; } = Color.White;

    public Color ShadowColor { get; init; } = Color.Transparent;

    public Vector2 ShadowOffset { get; init; } = Vector2.Zero;

    public bool AllowOverflow { get; init; } = false;

    public TextAlignment Alignment { get; init; } = TextAlignment.Left;


    public TextInfo() { }
}
