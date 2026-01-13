/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Author : TakoPuck (2026)                                                        *
 * Licence: You are free to use, modify, and distribute this code for any purpose. *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace CoolCustomText.Source;

public class PaletteRotator(ColorPalette colorPalette, float rotationTime)
{
    /// <summary>
    /// The different color palettes.
    /// Add as many as you want but don't forget to add the name of the new one in the enum 'ColorPalette'.
    /// </summary>
    private static readonly Dictionary<ColorPalette, Color[]> _palettes = new()
    {
        [ColorPalette.Rainbow] =
        [
            new(255, 0, 0),
            new(255, 135, 0),
            new(255, 211, 0),
            new(222, 255, 10),
            new(161, 255, 10),
            new(10, 255, 153),
            new(10, 239, 255),
            new(20, 125, 245),
            new(88, 10, 255),
            new(190, 10, 255)
        ],
        [ColorPalette.SoftCandy] =
        [
            new(245, 255, 198),
            new(180, 225, 255),
            new(171, 135, 255),
            new(255, 172, 228),
            new(193, 255, 155)
        ],
        [ColorPalette.SoftPurple] =
        [
            new(255, 214, 255),
            new(231, 198, 255),
            new(200, 182, 255),
            new(184, 192, 255),
            new(187, 208, 255)
        ],
        [ColorPalette.Retro] =
        [
            new(249, 65, 68),
            new(243, 114, 44),
            new(248, 150, 30),
            new(249, 199, 79),
            new(144, 190, 109),
            new(67, 170, 139),
            new(87, 117, 144)
        ],
        [ColorPalette.Elemental] =
        [
            new(84, 71, 140),
            new(44, 105, 154),
            new(4, 139, 168),
            new(13, 179, 158),
            new(22, 219, 147),
            new(131, 227, 119),
            new(185, 231, 105),
            new(239, 234, 90),
            new(241, 196, 83),
            new(242, 158, 76)
        ],
        [ColorPalette.White] =
        [
            new(255, 255, 255)
        ],
        [ColorPalette.TenMovingRed] =
        [
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            Color.Transparent,
            new(255, 0, 0)
        ],
        [ColorPalette.Transparent] =
        [
            Color.Transparent
        ]
    };

    private float _timer;
    private int _movingPos;
    private int _startingPos;
    private ColorPalette _colorPalette;
    private Color[] _palette = _palettes[colorPalette];


    public float RotationTime { get; set; } = rotationTime;

    public ColorPalette ColorPalette { get => _colorPalette; set { _colorPalette = value; _palette = _palettes[value]; } }

    public Color NextColor => _palette[(_startingPos + _movingPos++) % _palette.Length];


    public void RestartRotation() => _movingPos = 0;

    public void Update(float deltaTime)
    {
        if (_timer >= RotationTime)
        {
            _timer = 0;
            _startingPos = (_startingPos - 1 + _palette.Length) % _palette.Length;
        }

        _timer += deltaTime;
    }
}
