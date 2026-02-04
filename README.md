# Cool Custom Text (MonoGame)

![Visual example](Docs/CoolAnimatedText.gif "Visual exemple")

A small project that shows a cool way to use SpriteFont in MonoGame.  
Features: 
* Render text inside a fully customizable box (dimension scaling and padding)
* Apply special FX tags to style or animate parts of your text
* Customizable text shadow (color and offset)
* Handle text overflow seamlessly, with support for line-by-line or page-by-page display modes
* Text alignment: Left, Center or Right

---

## Changelog

1. Overflow managing
2. Side Step effect
3. Text alignment

---

## Important

To run the project, make sure to install the `.tff` font in `Content` folder.  

_This font was made by Zacchary Dempsey-Plante: https://www.dafont.com/fr/pixellari.font._

---

## Syntax and Application

To apply an effect to a specific part of the text, we use XML-like tag called 'fx'.  
In the visual example above, the input text looks like this :  
```csharp
string text = "Hello stranger, are you <fx 2,0,0,1,0>good</fx> ?\n<fx 1,1,0,0,0>*************************************</fx><fx 6,0,1,0,0>This line is scared</fx> <fx 6,1,0,0,0>></fx><fx 7,0,0,0,0>0123456789</fx><fx 6,1,0,0,0><</fx>";
```

As you can see, one fx tag contains 5 numbers that define a profile for the effect:  
`<fx Color Palette, Wave, Shake, Hang, Side Step>`

![Effects visual example](Docs/Effects.gif "Effects visual exemple")

Effects can be combine or can be ignored with 0.  

Custom texts support newlines (`\n`) and consecutives spaces.

Here, an example to know everything about custom texts:  
```csharp
SpriteFont font = Content.Load<SpriteFont>("PixellariFont");
_customText = new(_spriteBatch, font)
{
    Text = """
    Hello stranger, are you <fx 2,0,0,1,0>good</fx> <fx 0,1,0,0,0>?</fx>
    <fx 1,1,0,0,0>*************************************</fx>
    <fx 6,0,1,0,0>This line is scared</fx> <fx 6,0,0,0,1>></fx> <fx 7,0,0,0,0>0123456789</fx> <fx 6,0,0,0,2><</fx>
    """,
    Position = new(25f),
    Padding = new(5f, 0f),
    Dimension = new(284f, 60f),
    Scale = new(4f), // Scale the dimension and the padding to match pixels per unit from pixel art UI.
    Color = new(255, 244, 196),
    ShadowColor = new(128, 85, 111), // By default it's Color.Transparent which disable it.
    ShadowOffset = new(-2f, 2f),
    AllowOverflow = false, // Should the text overflows outside the box vertically ?
    Alignment = TextAlignment.Center
};

// If overflow is not allowed, use the following methods/properties to display the text:
// Page by page
_customText.CurrentPageIdx = 0;
_customText.NextPage();
_customText.PreviousPage();

// Line by line
_customText.StartingLineIdx = 0;
_customText.NextStartingLine();
_customText.PreviousStartingLine();

/* Another example where we copy the style of the last custom text */

font = Content.Load<SpriteFont>("SmallPixellariFont");
_infoCustomText = new(_spriteBatch, font, _customText)
{
    Text = """
    The gray box represents the text dimension.
    The text itself is rendered inside the green box because padding is applied.
    Overflow is enabled here, allowing the text to exceed the vertical bounds.
    All lines in this example are centered.
    Both of these behaviors can be changed.
    Newlines work as expected, and so do      consecutive spaces.
    To apply a <fx 5,1,0,1,0>special effect</fx> to part of the text, use the fx tag
    and configure the desired effect profiles (use zero to ignore an effect).
    FX tag syntax:
    <fx Color Palette profile, Wave profile, Shake p., Hang p., Side Step p.>text</fx>
    <fx 3,0,0,0,0>See README.md to learn everything about custom texts.</fx>
    """,
    Position = new(40f, 310f),
    Dimension = new(1200f, 92f),
    Scale = Vector2.One,
    Padding = new(0f, 10f),
    AllowOverflow = true,
};
```

---

## Add new profiles

Profiles are stored in static readonly dictionnaries in the nested class `FxText` in the class `CustomText`.  

To add a new profile for a specific effect, just follow the syntax.  

Here, what it looks like:

```csharp
// Palette color profiles
private readonly static Dictionary<int, Tuple<ColorPalette, float>> ColorProfiles = new()
{
    // Color Palette, Rotation Speed 
    [1] = new(ColorPalette.Rainbow, 0.075f),
    // New profile ! [2] = ...
}

// Wave profiles
private readonly static Dictionary<int, Tuple<float, float>> WaveProfils = new()
{
    // Wave Frequency, Wave Amplitude
    [1] = new(8f, 8f),
    // New profile ! [2] = ...
};

// Shake profiles
private readonly static Dictionary<int, Tuple<float, float>> ShakeProfils = new()
{
    // Shake Interval, Shake Strength
    [1] = new(0.06f, 3f),
    // New profile ! [2] = ...
};

// Hang profiles
private readonly static Dictionary<int, Tuple<float, float>> HangProfils = new()
{
    // Hang Frequency, Hang Amplitude
    [1] = new(6f, 12f),
    // New profile ! [2] = ....
};

// Side Step profiles
private readonly static Dictionary<int, Tuple<float, float>> SideStepProfils = new()
{
    // Side Step Frequency, Side Step Amplitude
    [1] = new(6f, 12f),
    [2] = new(6f, -12f)
};
```

The same applied to a new color palette.  
Profiles are stored in the class `PaletteRotator`, don't forget to add the name of the new color palette in the enum `ColorPalette`.

---

## License

You are free to use, modify, and distribute this code for any purpose.

