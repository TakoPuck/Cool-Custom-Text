# Cool Custom Text (MonoGame)

![Visual example](Docs/CoolAnimatedText.gif "Visual exemple")

A small project that shows a cool way to use SpriteFont in MonoGame.  
Features: 
* Render text inside a fully customizable box (dimension scaling and padding)
* Apply special FX tags to style or animate parts of your text
* Customizable text shadow (color and offset)
* Handle text overflow seamlessly, with support for line-by-line or page-by-page display modes

---

## Important

To run the project, make sure to install the `.tff` font in `Content` folder.  

_This font was made by Zacchary Dempsey-Plante: https://www.dafont.com/fr/pixellari.font._

---

## Syntax and Application

To apply an effect to a specific part of the text, we use XML-like tag called 'fx'.  
In the visual exemple above the input text looks like this :  
```csharp
string text = "Hello stranger, are you <fx 2,0,0,1,0>good</fx> ?\n<fx 1,1,0,0,0>*************************************</fx><fx 6,0,1,0,0>This line is scared</fx> <fx 6,1,0,0,0>></fx><fx 7,0,0,0,0>0123456789</fx><fx 6,1,0,0,0><</fx>";
```

As you can see, one fx tag contains 5 numbers that define a profile for the effect:  
<fx Color Palette, Wave, Shake, Hang, Side Step>  

![Effects visual example](Docs/Effects.gif "Effects visual exemple")

Effects can be combine or can be ignored with 0.  
Custom texts support newlines and consecutives spaces.

Here, how to create and manage a custom text:  
```csharp
string text = "This my <fx 2,0,0,1,0>cool custom text</fx> !";
Vector2 textDim = new(284f, 60f);
Vector2 position = new(50f, 50f);

_customText = new CustomText(this, "PixellariFont", text, position, textDim) // 'this' is Game1
{
    Scale = new(4f), // Scale the dimension. This is useful if you're working with pixel art UI to match the pixel per unit and have the dimension (& padding) based on pixel art size.
    Color = new(255, 244, 196),
    Padding = new(5f, 0f),
    ShadowColor = new(128, 85, 111), // By default it's Color.Transparent which disable it.
    // ShadowOffset can be changed too.
};
// Don't forget to refresh the text after the initialization and after you change the text properties except those related to overflow (see below).
_customText.Refresh();

// When not allowing overflow, use the following methods to draw the overflowing text:
// Note that calling Refresh is not required to apply overflow changes.
_customText.AllowOverflow = false;

// Page by page
_customText.CurrentPageIdx = 0;
_customText.NextPage();
_customText.PreviousPage();

// Line by line
_customText.StartingLineIdx = 0;
_customText.NextStartingLine();
_customText.PreviousStartingLine();
```

Don't forget to update and draw your custom text:
```csharp
protected override void Update(GameTime gameTime)
{
    ...
    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _customText.Update(deltaTime);
    ...    
}

protected override void Draw(GameTime gameTime)
{
    ...
    _customText.Draw();
    ...
}
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
public static Dictionary<int, Tuple<float, float>> ShakeProfils = new()
{
    // Shake Interval, Shake Strength
    [1] = new(0.06f, 3f),
    // New profile ! [2] = ...
};

// Hang profiles
public static Dictionary<int, Tuple<float, float>> HangProfils = new()
{
    // Hang Frequency, Hang Amplitude
    [1] = new(6f, 12f),
    // New profile ! [2] = ....
};

// Side Step profiles
public static Dictionary<int, Tuple<float, float>> SideStepProfils = new()
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

Free to use for any purpose. 
