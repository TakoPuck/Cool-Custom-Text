/* * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  Author: TakoPuck (2025)                            *
 *  This code is open and free to use for any purpose. *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using Microsoft.Xna.Framework.Graphics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Text;
using System;

namespace CoolCustomText.Source;

public partial class CustomText
{
    private readonly SpriteBatch _spriteBatch;
    private readonly float _lineHeight;

    private string[][] _noFxTexts;
    private FxText[] _fxTexts;
    private float _time;
    private int _lineCapacity;
    private int _currentLineIdx;
    private bool _allowOverflow;
    private int _startingLineIdx;

    #region Properties

    public SpriteFont Font { get; set; }

    public string Text { get; set; }

    public Color Color { get; set; }

    public Color ShadowColor { get; set; }

    public Vector2 ShadowOffset { get; set; }

    /// <remarks>
    /// Dimension is affected by <see cref="Scale"/>.
    /// </remarks>
    public Vector2 Dimension { get; set; }

    /// <summary>
    /// The origin of the text is (0,0), so the position refers to the top left position.
    /// </summary>
    public Vector2 Position { get; set; }

    public Vector2 Offset { get; set; }

    /// <remarks>
    /// Padding is affected by <see cref="Scale"/>.
    /// </remarks>
    public Vector2 Padding { get; set; }

    /// <summary>
    /// Scale of the dimension and the padding, the font size is not affected.
    /// </summary>
    /// <remarks>
    /// To change the font size, you need to edit your spritefont file.
    /// </remarks>
    public Vector2 Scale { get; set; }

    public bool AllowOverflow
    {
        get => _allowOverflow;
        set { _allowOverflow = value; StartingLineIdx = 0; }
    }

    public int LineCount { get; private set; }

    /// <summary>
    /// Index of the first drawn line.<br></br>
    /// If <see cref="AllowOverflow"/> is disabled, then the value is always 0.
    /// </summary>
    public int StartingLineIdx
    {
        get => _startingLineIdx;
        set { _startingLineIdx = AllowOverflow ? 0 : Math.Clamp(value, 0, Math.Max(0, LineCount - 1)); }
    }

    public bool HasNextLine => StartingLineIdx < LineCount - 1;

    public bool HasPreviousLine => StartingLineIdx > 0;

    public int PageCount => _lineCapacity == 0 ? 0 : LineCount / _lineCapacity;

    /// <summary>
    /// Index of the current drawn page.<br></br>
    /// If <see cref="AllowOverflow"/> is disabled, then the value is always 0.
    /// </summary>
    public int CurrentPageIdx
    {
        get => _lineCapacity == 0 ? 0 : StartingLineIdx / _lineCapacity;
        set => StartingLineIdx = value * _lineCapacity;
    }

    public bool HasNextPage => CurrentPageIdx < PageCount - 1;

    public bool HasPreviousPage => CurrentPageIdx > 0;

    #endregion

    public CustomText(Game game, string fontName, string text, Vector2 position, Vector2 dimension, Vector2 offset = default, Vector2 padding = default,
        Vector2? scale = null, Color? color = null, Color? shadowColor = null, Vector2? shadowOffset = null, bool allowOverflow = false)
    {
        Font = game.Content.Load<SpriteFont>(fontName);
        ShadowColor = shadowColor ?? Color.Transparent;
        ShadowOffset = shadowOffset ?? new(-4f, 4f);
        Color = color ?? Color.White;
        AllowOverflow = allowOverflow;
        Dimension = dimension;
        Position = position;
        Padding = padding;
        Offset = offset;
        Text = text;
        Scale = scale ?? Vector2.One;

        _lineHeight = Font.MeasureString(" ").Y;
        _spriteBatch = game.Services.GetService<SpriteBatch>();
    }

    #region Private methods

    [GeneratedRegex(@"<fx\s+([0-9,]+)>(.*?)</fx>", RegexOptions.Singleline)]
    private static partial Regex FxTextRegex();

    #region Output building related

    private string BuildOutput(List<string> words, List<int> subwordIdxsExcludingFirst, out List<int> addedChars)
    {
        StringBuilder line = new();
        StringBuilder output = new();
        addedChars = []; // Used to adjust the indexes of fx texts.

        for (int i = 0; i < words.Count; i++)
        {
            string testLine = line.Length == 0 ? words[i] : line + " " + words[i];

            if (Font.MeasureString(testLine).X > (Dimension.X - 2f * Padding.X) * Scale.X)
            {
                if (i > 0) line.Append('\n');

                output.Append(line);

                // A new line was inserted between two parts of the same long word,
                // which increases the output length, unlike a regular new line that
                // simply replaces a space between two separate words.
                if (subwordIdxsExcludingFirst.Contains(i))
                    addedChars.Add(output.Length - 1);

                line.Clear();

                // A word can be empty if it's from consecutives spaces, in this case we add a space to the output.
                if (words[i] == string.Empty)
                {
                    line.Append(' ');
                    addedChars.Add(output.Length);
                }
                else line.Append(words[i]);
            }
            else
            {
                // Add a space between words unless the current word start the line.
                if (line.Length > 0) line.Append(' ');

                line.Append(words[i]);
            }
        }

        if (line.Length > 0) output.Append(line);

        return output.ToString();
    }

    private List<string> SliceLongWord(string longWord)
    {
        List<string> words = [];
        StringBuilder word = new();

        foreach (char c in longWord)
        {
            word.Append(c);

            if (Font.MeasureString(word).X > (Dimension.X - 2f * Padding.X) * Scale.X)
            {
                word.Remove(word.Length - 1, 1);
                words.Add(word.ToString());
                word.Clear();
                word.Append(c);
            }
        }

        words.Add(word.ToString());

        return words;
    }

    private List<string> ProcessRawWords(string[] rawWords, out List<int> subwordIdxsExcludingFirst)
    {
        List<string> processedWords = [];
        subwordIdxsExcludingFirst = [];

        foreach (string word in rawWords)
        {
            float wordWidth = Font.MeasureString(word).X;

            // Slice words that are longer than a line.
            if (wordWidth > (Dimension.X - 2f * Padding.X) * Scale.X)
            {
                List<string> longWordsParts = SliceLongWord(word);
                for (int i = 0; i < longWordsParts.Count; i++)
                {
                    string part = longWordsParts[i];

                    // Don't add the idx of the first subword.
                    if (i != 0) subwordIdxsExcludingFirst.Add(processedWords.Count);

                    processedWords.Add(part);
                }
            }
            else processedWords.Add(word);
        }

        return processedWords;
    }

    private string BuildFxTexts(string text)
    {
        Regex regex = FxTextRegex();
        var matches = regex.Matches(text);
        int ignoredCharCount = 0;

        _fxTexts = new FxText[matches.Count];

        // Process all extracted fx texts.
        for (int i = 0; i < matches.Count; i++)
        {
            Match m = matches[i];

            string[] values = m.Groups[1].Value.Split(',');
            string innerText = m.Groups[2].Value;

            int startIdx = m.Index - ignoredCharCount;
            int colorProfil = 0, waveProfil = 0, shakeProfil = 0, hangProfil = 0, sideStepProfil = 0;

            if (values.Length == 5)
            {
                _ = int.TryParse(values[0], out colorProfil);
                _ = int.TryParse(values[1], out waveProfil);
                _ = int.TryParse(values[2], out shakeProfil);
                _ = int.TryParse(values[3], out hangProfil);
                _ = int.TryParse(values[4], out sideStepProfil);
            }

            _fxTexts[i] = new(startIdx, innerText.Length, colorProfil, waveProfil, shakeProfil, hangProfil, sideStepProfil);

            ignoredCharCount += m.Length - innerText.Length;
        }

        // Return the text without fx tags.
        return regex.Replace(text, m => m.Groups[2].Value);
    }

    private string FilterUnsupportedChars(string text)
    {
        StringBuilder sb = new();

        foreach (char c in text)
            sb.Append((!Font.Characters.Contains(c) && (c != '\n')) ? '?' : c);

        return sb.ToString();
    }

    #endregion
    #region Output post-building related

    private void AdjustFxTextsIndexes(List<int> addedChars)
    {
        if (addedChars.Count == 0) return;

        foreach (FxText fxText in _fxTexts)
        {
            foreach (int pos in addedChars)
            {
                // A char was added in the fx text, so we update the fx text's length.
                if (pos >= fxText.StartIdx && pos <= fxText.EndIdx)
                    fxText.Length++;

                // A char was added before the start of the fx text, so we increase the start index.
                else if (pos < fxText.StartIdx)
                    fxText.StartIdx++;
            }
        }
    }

    private void BuildNoFxTexts(string output)
    {
        int startIdx = 0;

        _noFxTexts = new string[_fxTexts.Length + 1][];

        // All no-fx texts are separated by an fx text. example: [Hello stranger, are you ]{good}[ ?]
        for (int i = 0; i < _fxTexts.Length; i++)
        {
            int length = _fxTexts[i].StartIdx - startIdx;
            string noFxPart = length > 0 ? output.Substring(startIdx, length) : string.Empty;

            // Split into lines.
            _noFxTexts[i] = noFxPart.Split('\n');

            startIdx = _fxTexts[i].EndIdx + 1;
        }

        // Don't forget to add the last one. (in the example it's: [ ?])
        _noFxTexts[^1] = output.Substring(startIdx).Split('\n');
    }

    public void CountLines()
    {
        LineCount = 1;

        Vector2 nextCharPos = Position + Padding * Scale;

        for (int i = 0; i < _noFxTexts.Length; i++)
        {
            nextCharPos = CountPartLines(_noFxTexts[i], nextCharPos);

            if (_fxTexts.Length > 0 && i < _fxTexts.Length)
                nextCharPos = CountPartLines(_fxTexts[i].Lines, nextCharPos);
        }
    }

    private Vector2 CountPartLines(string[] lines, Vector2 nextCharPos)
    {
        float initialLineStartX = nextCharPos.X;

        for (int j = 1; j < lines.Length; j++)
        {
            nextCharPos = new(Position.X + Padding.X * Scale.X, nextCharPos.Y + _lineHeight);
            LineCount++;
        }

        if (lines[^1] != string.Empty)
        {
            float lastLineStartX = (lines.Length == 1) ? initialLineStartX : Position.X + Padding.X * Scale.X;
            nextCharPos = new(lastLineStartX + Font.MeasureString(lines[^1]).X, nextCharPos.Y);
        }

        return nextCharPos;
    }

    #endregion
    #region Draw related

    private Vector2 GetNextFxCharPosition(int lineLength, Vector2 nextCharPos, FxText fxText)
    {
        return new Vector2()
        {
            X = nextCharPos.X +
                (fxText.Shake ? MathF.Sin(fxText.Rand.Next()) * fxText.ShakeStrength : 0f) +
                (fxText.SideStep ? MathF.Sin(_time * fxText.SideStepFrequency + lineLength) * fxText.SideStepAmplitude : 0f),

            Y = nextCharPos.Y +
                (fxText.Shake ? MathF.Sin(fxText.Rand.Next()) * fxText.ShakeStrength : 0f) +
                (fxText.Wave ? MathF.Sin(_time * fxText.WaveFrequency + lineLength) * fxText.WaveAmplitude : 0f)
        };
    }

    private void DrawFxTextLine(FxText fxText, int lineIdx, Vector2 nextCharPos)
    {
        float charWidth;
        int lineLength = 0;
        Vector2 nextFxCharPos = GetNextFxCharPosition(lineLength, nextCharPos, fxText);

        if (fxText.Shake) fxText.ResetRand();

        for (int i = 0; i < fxText.Lines[lineIdx].Length; i++)
        {
            char c = fxText.Lines[lineIdx][i];
            Color color = fxText.PaletteRotator?.NextColor ?? Color;
            Vector2 origin = Vector2.Zero;
            float rotation = 0f;

            if (fxText.Hang)
            {
                origin = new(Font.MeasureString(c.ToString()).X / 2f, 0f);
                rotation = MathHelper.ToRadians(MathF.Sin(_time * fxText.HangFrequency + i) * fxText.HangAmplitude);
                nextFxCharPos = new(nextFxCharPos.X + origin.X, nextFxCharPos.Y);
            }

            Color shadowColor = color == Color ? ShadowColor : new(color.ToVector4() * 0.45f + Vector4.UnitW * 255f * color.A);
            DrawString(c.ToString(), nextFxCharPos, color, rotation, origin, shadowColor);

            lineLength++;
            charWidth = Font.MeasureString(c.ToString()).X;

            nextCharPos = new(nextCharPos.X + charWidth, nextCharPos.Y);
            nextFxCharPos = GetNextFxCharPosition(lineLength, nextCharPos, fxText);
        }

        fxText.PaletteRotator?.RestartRotation();
    }

    private void DrawString(string text, Vector2 position, Color color, float rotation = 0f, Vector2 origin = default, Color? shadowColor = null)
    {
        // Draw text shadow.
        if (ShadowColor != Color.Transparent)
            _spriteBatch.DrawString(Font, text, position + ShadowOffset, shadowColor ?? ShadowColor, rotation, origin, 1f, SpriteEffects.None, 0f);

        _spriteBatch.DrawString(Font, text, position, color, rotation, origin, 1f, SpriteEffects.None, 0f);
    }

    private Vector2 DrawLines(string[] lines, Vector2 nextCharPos, FxText fxText = null)
    {
        float initialLineStartX = nextCharPos.X;

        // Draw the first line at the given nextCharPos.
        if (IsLineDrawable(_currentLineIdx) && lines[0] != string.Empty)
        {
            Vector2 v = new(nextCharPos.X, Position.Y + Padding.Y * Scale.Y + _lineHeight * (_currentLineIdx - StartingLineIdx));

            if (fxText != null)
                DrawFxTextLine(fxText, 0, v);
            else
                DrawString(lines[0], v, Color);
        }

        // Then, draw the subsequent lines at their respective positions.
        for (int j = 1; j < lines.Length; j++)
        {
            nextCharPos = new(Position.X + Padding.X * Scale.X, nextCharPos.Y + _lineHeight);
            _currentLineIdx++;

            if (IsLineDrawable(_currentLineIdx) && lines[j] != string.Empty)
            {
                Vector2 v = new(nextCharPos.X, Position.Y + Padding.Y * Scale.Y + _lineHeight * (_currentLineIdx - StartingLineIdx));

                if (fxText != null)
                    DrawFxTextLine(fxText, j, v);
                else
                    DrawString(lines[j], v, Color);
            }
        }

        // Finally, determine the start X of the last drawn line to get the next char position:
        // if there's only one line, it started at initialLineStartX, otherwise, it started at the beginning of a line.
        if (lines[^1] != string.Empty)
        {
            float lastLineStartX = (lines.Length == 1) ? initialLineStartX : Position.X + Padding.X * Scale.X;
            nextCharPos = new(lastLineStartX + Font.MeasureString(lines[^1]).X, nextCharPos.Y);
        }

        return nextCharPos;
    }

    private bool IsLineDrawable(int lineIdx) => AllowOverflow || ((lineIdx >= StartingLineIdx) && (lineIdx < StartingLineIdx + _lineCapacity));

    #endregion
    #endregion
    #region Public methods

    public void Draw()
    {
        _currentLineIdx = 0;

        Vector2 nextCharPos = Position + Padding * Scale;

        // All no-fx texts are separated by an fx text.
        for (int i = 0; i < _noFxTexts.Length; i++)
        {
            // Draw no-fx lines.
            nextCharPos = DrawLines(_noFxTexts[i], nextCharPos);

            // Then draw fx lines, if any.
            if (_fxTexts.Length > 0 && i < _fxTexts.Length)
                nextCharPos = DrawLines(_fxTexts[i].Lines, nextCharPos, _fxTexts[i]);
        }
    }

    public void Update(float deltaTime)
    {
        foreach (var fxText in _fxTexts)
            fxText.Update(deltaTime);

        _time = (_time + deltaTime) % 3600f;
    }

    public void Refresh()
    {
        string filteredText = FilterUnsupportedChars(Text);
        string noTagsText = BuildFxTexts(filteredText);
        string[] rawWords = noTagsText.Split(' ');

        // Raw words can be longer than a line, that's why we process them.
        List<string> words = ProcessRawWords(rawWords, out List<int> subwordIdxsExcludingFirst);

        // Build output by placing the words within the set dimension.
        string output = BuildOutput(words, subwordIdxsExcludingFirst, out List<int> addedChars);

        // Adjust the indexes of fx texts based on the added chars that increase the output's length.
        AdjustFxTextsIndexes(addedChars);

        // Build the lines of fx texts according to their indexes.
        foreach (FxText fxText in _fxTexts)
            fxText.Lines = output.Substring(fxText.StartIdx, fxText.Length).Split('\n');

        BuildNoFxTexts(output);

        CountLines();
        StartingLineIdx = 0;
        _lineCapacity = (int)(Dimension.Y * Scale.Y / _lineHeight);
    }

    public void NextStartingLine()
    {
        StartingLineIdx = AllowOverflow ? 0 : Math.Min(StartingLineIdx + 1, LineCount - 1);
    }

    public void PreviousStartingLine()
    {
        StartingLineIdx = AllowOverflow ? 0 : Math.Max(0, StartingLineIdx - 1);
    }

    public void NextPage()
    {
        int maxLineIdx = Math.Max(0, LineCount - _lineCapacity);
        StartingLineIdx = AllowOverflow ? 0 : Math.Min(StartingLineIdx + _lineCapacity, maxLineIdx);
    }

    public void PreviousPage()
    {
        StartingLineIdx = AllowOverflow ? 0 : Math.Max(0, StartingLineIdx - _lineCapacity);
    }

    public Vector2 GetAutoDimension()
    {
        string cleanedText = FxTextRegex().Replace(Text, m => m.Groups[2].Value);
        return (Font.MeasureString(cleanedText) + Padding * 2) / Scale;
    }

    #endregion

    /// <summary>
    /// Nested class that defines an fx text.
    /// </summary>
    private class FxText
    {
        /// <summary>
        /// The different color palette profiles. Add as many as you want by following the syntax below.
        /// </summary>
        private readonly static Dictionary<int, Tuple<ColorPalette, float>> ColorProfiles = new()
        {
            // Color Palette, Rotation Speed 
            [1] = new(ColorPalette.Rainbow, 0.075f),
            [2] = new(ColorPalette.Elemental, 0.075f),
            [3] = new(ColorPalette.SoftCandy, 0.075f),
            [4] = new(ColorPalette.SoftPurple, 0.075f),
            [5] = new(ColorPalette.Retro, 0.075f),
            [6] = new(ColorPalette.White, 0.075f),
            [7] = new(ColorPalette.TenMovingRed, 0.125f)
        };

        /// <summary>
        /// The different wave profiles. Add as many as you want by following the syntax below.
        /// </summary>
        private readonly static Dictionary<int, Tuple<float, float>> WaveProfils = new()
        {
            // Wave Frequency, Wave Amplitude
            [1] = new(8f, 8f)
        };

        /// <summary>
        /// The different shake profiles. Add as many as you want by following the syntax below.
        /// </summary>
        public static Dictionary<int, Tuple<float, float>> ShakeProfils = new()
        {
            // Shake Interval, Shake Strength
            [1] = new(0.06f, 3f),
        };

        /// <summary>
        /// The different hang profiles. Add as many as you want by following the syntax below.
        /// </summary>
        public static Dictionary<int, Tuple<float, float>> HangProfils = new()
        {
            // Hang Frequency, Hang Amplitude
            [1] = new(6f, 12f)
        };

        /// <summary>
        /// The different side step profiles. Add as many as you want by following the syntax below.
        /// </summary>
        public static Dictionary<int, Tuple<float, float>> SideStepProfils = new()
        {
            // Side Step Frequency, Side Step Amplitude
            [1] = new(6f, 12f),
            [2] = new(6f, -12f)
        };


        private float _shakeTime;
        private int _randSeed;


        public int Length { get; set; }

        public int StartIdx { get; set; }

        public string[] Lines { get; set; }

        public PaletteRotator PaletteRotator { get; }

        public bool Wave { get; }

        public float WaveFrequency { get; }

        public float WaveAmplitude { get; }

        public bool Hang { get; }

        public float HangFrequency { get; }

        public float HangAmplitude { get; }

        public bool Shake { get; }

        public float ShakeInterval { get; }

        public float ShakeStrength { get; }

        public bool SideStep { get; }

        public float SideStepFrequency { get; }

        public float SideStepAmplitude { get; }

        public Random Rand { get; private set; }

        public int EndIdx => StartIdx + Length - 1;


        public FxText(int startIdx, int length, int colorProfil, int waveProfil, int shakeProfil, int hangProfil, int sideStepProfil)
        {
            Length = length;
            StartIdx = startIdx;

            if (ColorProfiles.TryGetValue(colorProfil, out Tuple<ColorPalette, float> colorValues))
                PaletteRotator = new(colorValues.Item1, colorValues.Item2);

            if (WaveProfils.TryGetValue(waveProfil, out Tuple<float, float> waveValues))
            {
                (WaveFrequency, WaveAmplitude) = waveValues;
                Wave = true;
            }

            if (ShakeProfils.TryGetValue(shakeProfil, out Tuple<float, float> shakeValues))
            {
                (ShakeInterval, ShakeStrength) = shakeValues;
                _randSeed = (int)DateTime.Now.Ticks;
                ResetRand();
                Shake = true;
            }

            if (HangProfils.TryGetValue(hangProfil, out Tuple<float, float> hangValues))
            {
                (HangFrequency, HangAmplitude) = hangValues;
                Hang = true;
            }

            if (SideStepProfils.TryGetValue(sideStepProfil, out Tuple<float, float> sideStepValues))
            {
                (SideStepFrequency, SideStepAmplitude) = sideStepValues;
                SideStep = true;
            }
        }

        public void Update(float deltaTime)
        {
            PaletteRotator?.Update(deltaTime);

            if (Shake)
            {
                _shakeTime += deltaTime;

                if (_shakeTime >= ShakeInterval)
                {
                    _shakeTime = 0f;
                    _randSeed = (int)DateTime.Now.Ticks;
                }
            }
        }

        public void ResetRand() => Rand = new Random(_randSeed);
    }
}
