/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Author : TakoPuck (2026)                                                        *
 * Licence: You are free to use, modify, and distribute this code for any purpose. *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

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

    private float[] _alignedLineStartsX;
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

    public TextAlignment Alignment { get; set; }

    public bool AllowOverflow
    {
        get => _allowOverflow;
        set { _allowOverflow = value; StartingLineIdx = 0; }
    }

    public int LineCount { get; private set; }

    /// <summary>
    /// Index of the first drawn line.<br></br>
    /// If <see cref="AllowOverflow"/> is enabled, then the value is always 0.
    /// </summary>
    public int StartingLineIdx
    {
        get => _startingLineIdx;
        set { _startingLineIdx = AllowOverflow ? 0 : Math.Clamp(value, 0, Math.Max(0, LineCount - 1)); }
    }

    public bool HasNextLine => StartingLineIdx < LineCount - 1;

    public bool HasPreviousLine => StartingLineIdx > 0;

    public int PageCount => _lineCapacity == 0 ? 0 : (LineCount + _lineCapacity - 1) / _lineCapacity;

    /// <summary>
    /// Index of the current drawn page.<br></br>
    /// If <see cref="AllowOverflow"/> is enabled, then the value is always 0.
    /// </summary>
    public int CurrentPageIdx
    {
        get => _lineCapacity == 0 ? 0 : StartingLineIdx / _lineCapacity;
        set => StartingLineIdx = value * _lineCapacity;
    }

    public bool HasNextPage => CurrentPageIdx < PageCount - 1;

    public bool HasPreviousPage => CurrentPageIdx > 0;

    #endregion

    public CustomText(Game game, string fontName, string text, Vector2 position, Vector2 dimension, Vector2 padding = default,
        Vector2? scale = null, Color? color = null, Color? shadowColor = null, Vector2? shadowOffset = null, bool allowOverflow = false, TextAlignment alignment = TextAlignment.Left)
        : this(game.Services.GetService<SpriteBatch>(), game.Content.Load<SpriteFont>(fontName),
        text, position, dimension, padding, scale, color, shadowColor, shadowOffset, allowOverflow, alignment)
    { }

    public CustomText(SpriteBatch sb, SpriteFont font, string text, Vector2 position, Vector2 dimension, Vector2 padding = default,
        Vector2? scale = null, Color? color = null, Color? shadowColor = null, Vector2? shadowOffset = null, bool allowOverflow = false, TextAlignment alignment = TextAlignment.Left)
    {
        ShadowColor = shadowColor ?? Color.Transparent;
        ShadowOffset = shadowOffset ?? new(-4f, 4f);
        Scale = scale ?? Vector2.One;
        Color = color ?? Color.White;
        AllowOverflow = allowOverflow;
        Alignment = alignment;
        Dimension = dimension;
        Position = position;
        Padding = padding;
        Text = text;
        Font = font;

        _lineHeight = Font.MeasureString(" ").Y;
        _spriteBatch = sb;

        Refresh();
    }

    #region Private methods
    #region Regex

    [GeneratedRegex(@"<fx\s+(\d+),(\d+),(\d+),(\d+),(\d+)>(.*?)</fx>", RegexOptions.Singleline)]
    private static partial Regex FxTextRegex();

    [GeneratedRegex(@"^( )")]
    private static partial Regex StartWithSpaceRegex();

    [GeneratedRegex(@"^(<fx\s+\d+,\d+,\d+,\d+,\d+>)(\s)(.*?</fx>)")]
    private static partial Regex StartWithFxSpaceRegex();

    #endregion
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

            int startIdx = m.Index - ignoredCharCount;

            int colorProfil    = int.Parse(m.Groups[1].Value);
            int waveProfil     = int.Parse(m.Groups[2].Value);
            int shakeProfil    = int.Parse(m.Groups[3].Value);
            int hangProfil     = int.Parse(m.Groups[4].Value);
            int sideStepProfil = int.Parse(m.Groups[5].Value);
            string innerText   = m.Groups[6].Value;

            _fxTexts[i] = new(startIdx, innerText.Length, colorProfil, waveProfil, shakeProfil, hangProfil, sideStepProfil);

            ignoredCharCount += m.Length - innerText.Length;
        }

        // Return the text without fx tags.
        return regex.Replace(text, m => m.Groups[6].Value);
    }

    private string FilterUnsupportedChars(string text)
    {
        StringBuilder sb = new();

        foreach (char c in text)
            sb.Append((!Font.Characters.Contains(c) && (c != '\n')) ? '?' : c);

        return sb.ToString();
    }

    private static string FixStartingSpace(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            text = StartWithSpaceRegex().Replace(text, "<fx 8,0,0,0,0>.</fx>");
            text = StartWithFxSpaceRegex().Replace(text, m => string.Concat("<fx 8,0,0,0,0>.</fx>", m.Groups[1].Value, m.Groups[3].Value));
        }

        return text;
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

    #region Text alignment

    private string[] GetSimulatedText()
    {
        List<StringBuilder> simulatedText = [];

        for (int i = 0; i < _noFxTexts.Length; i++)
        {
            SimulateTextPart(_noFxTexts[i], simulatedText);

            if (i < _fxTexts.Length)
            {
                SimulateTextPart(_fxTexts[i].Lines, simulatedText);
            }
        }

        // Build result
        string[] result = new string[simulatedText.Count];
        int lineIdx = 0;
        simulatedText.ForEach(sb => result[lineIdx++] = sb.ToString());

        return result;
    }

    private static void SimulateTextPart(string[] lines, List<StringBuilder> simulatedText)
    {
        if (simulatedText.Count == 0)
        {
            simulatedText.Add(new());
        }

        simulatedText[^1].Append(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            simulatedText.Add(new(lines[i]));
        }
    }

    private void ComputeAlignedLineStartsX()
    {
        string[] lines = GetSimulatedText();

        _alignedLineStartsX = new float[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            _alignedLineStartsX[i] = GetAlignedLineStartX(lines[i]);
        }
    }

    private float GetAlignedLineStartX(string line)
    {
        float lineWidth = Font.MeasureString(line).X;
        float boxWidth = (Dimension.X - 2f * Padding.X) * Scale.X;
        float baseX = Position.X + Padding.X * Scale.X;

        return Alignment switch
        {
            TextAlignment.Center => baseX + (boxWidth - lineWidth) / 2f,
            TextAlignment.Right => baseX + (boxWidth - lineWidth),
            _ => baseX // Left
        };
    }

    #endregion
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

    private float DrawLines(string[] lines, float nextLineStartX, FxText fxText = null)
    {
        float initialLineStartX = nextLineStartX;
        Vector2 nextCharPos;

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) _currentLineIdx++;

            nextCharPos = new()
            {
                X = (i == 0) ? initialLineStartX : _alignedLineStartsX[_currentLineIdx],
                Y = Position.Y + Padding.Y * Scale.Y + _lineHeight * (_currentLineIdx - StartingLineIdx)
            };

            if (IsLineDrawable(_currentLineIdx) && lines[i] != string.Empty)
            {
                if (fxText != null)
                {
                    DrawFxTextLine(fxText, i, nextCharPos);
                }
                else
                {
                    DrawString(lines[i], nextCharPos, Color);
                }
            }
        }

        if (lines[^1] == string.Empty && lines.Length > 1)
        {
            return _alignedLineStartsX[_currentLineIdx];
        }

        float lastLineStartX = (lines.Length == 1) ? initialLineStartX : _alignedLineStartsX[_currentLineIdx];

        return lastLineStartX + Font.MeasureString(lines[^1]).X;
    }

    private bool IsLineDrawable(int lineIdx) => AllowOverflow || ((lineIdx >= StartingLineIdx) && (lineIdx < StartingLineIdx + _lineCapacity));

    #endregion
    #endregion
    #region Public methods

    public void Draw()
    {
        _currentLineIdx = 0;

        float nextLineStartX = _alignedLineStartsX[0];

        // All no-fx texts are separated by an fx text.
        for (int i = 0; i < _noFxTexts.Length; i++)
        {
            // Draw no-fx lines.
            nextLineStartX = DrawLines(_noFxTexts[i], nextLineStartX);

            // Then draw fx lines, if any.
            if (i < _fxTexts.Length)
            {
                nextLineStartX = DrawLines(_fxTexts[i].Lines, nextLineStartX, _fxTexts[i]);
            }
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
        string safeText = FixStartingSpace(Text); // Workaround: Replace first leading space with a transparent fx tag to avoid crashes...
        string filteredText = FilterUnsupportedChars(safeText);
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

        ComputeAlignedLineStartsX();

        StartingLineIdx = 0;
        LineCount = _alignedLineStartsX.Length;
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
            [7] = new(ColorPalette.TenMovingRed, 0.125f),
            [8] = new(ColorPalette.Transparent, 0f)
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
        private readonly static Dictionary<int, Tuple<float, float>> ShakeProfils = new()
        {
            // Shake Interval, Shake Strength
            [1] = new(0.06f, 3f),
        };

        /// <summary>
        /// The different hang profiles. Add as many as you want by following the syntax below.
        /// </summary>
        private readonly static Dictionary<int, Tuple<float, float>> HangProfils = new()
        {
            // Hang Frequency, Hang Amplitude
            [1] = new(6f, 12f)
        };

        /// <summary>
        /// The different side step profiles. Add as many as you want by following the syntax below.
        /// </summary>
        private readonly static Dictionary<int, Tuple<float, float>> SideStepProfils = new()
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
