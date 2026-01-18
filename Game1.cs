/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Author : TakoPuck (2026)                                                        *
 * Licence: You are free to use, modify, and distribute this code for any purpose. *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using CoolCustomText.Source;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CoolCustomText
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        private SpriteBatch _spriteBatch;
        private CustomText _customText;
        private CustomText _infoCustomText;
        private Texture2D _pixelTex;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Services.AddService(_spriteBatch);

            /* Example of using Custom Text */

            SpriteFont font = Content.Load<SpriteFont>("PixellariFont");
            TextInfo info = new()
            {
                Text = """
                Hello stranger, are you <fx 2,0,0,1,0>good</fx> <fx 0,1,0,0,0>?</fx>
                <fx 1,1,0,0,0>*************************************</fx>
                <fx 6,0,1,0,0>This line is scared</fx> <fx 6,0,0,0,1>></fx> <fx 7,0,0,0,0>0123456789</fx> <fx 6,0,0,0,2><</fx>
                """,
                Position      = new(25f),
                Padding       = new(5f, 0f),
                Dimension     = new(284f, 60f),
                Scale         = new(4f), // Scale the dimension and the padding to match pixels per unit from pixel art UI.
                Color         = new(255, 244, 196),
                ShadowColor   = new(128, 85, 111), // By default it's Color.Transparent which disable it.
                ShadowOffset  = new(-2f, 2f),
                AllowOverflow = false, // Should the text overflows outside the box vertically ?
                Alignment     = TextAlignment.Center
            };

            _customText = new(_spriteBatch, font, info);

            // Refresh should be call when editing the following properties:
            // Font - Text - Dimension - Position - Padding - Scale - Alignment
            _customText.Position = new(50f);
            _customText.Refresh();

            // Refresh should not be call when editing the following properties:
            // Color - ShadowColor - ShadowOffset - AllowOverflow - CurrentPageIdx - StartingLineIdx
            _customText.ShadowOffset = new(-4f, 4f);

            // If overflow is not allowed, use the following methods/properties to display the text:

            // Page by page
            _customText.CurrentPageIdx = 0;
            _customText.NextPage();
            _customText.PreviousPage();

            // Line by line
            _customText.StartingLineIdx = 0;
            _customText.NextStartingLine();
            _customText.PreviousStartingLine();

            /* Another example by copying the previous text info and making some changes. */

            font = Content.Load<SpriteFont>("SmallPixellariFont");
            info = info with
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
                Position      = new(40f, 310f),
                Dimension     = new(1200f, 92f),
                Scale         = Vector2.One,
                Padding       = new(0f, 10f),
                AllowOverflow = true,
                Alignment     = TextAlignment.Center
            };

            _infoCustomText = new(_spriteBatch, font, info);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _pixelTex = Content.Load<Texture2D>("WhitePixel");
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _customText.Update(deltaTime);
            _infoCustomText.Update(deltaTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawCustomTextBounds(_customText);
            _customText.Draw();

            DrawCustomTextBounds(_infoCustomText);
            _infoCustomText.Draw();

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draw the bounds of a custom text to have a visual debug.
        /// </summary>
        /// <param name="t">The custom text.</param>
        public void DrawCustomTextBounds(CustomText t)
        {
            Color dimColor = new(64, 64, 64, 64);
            Color paddingColor = new(0, 64, 0, 64);
            Vector2 scale = t.Dimension * t.Scale;

            _spriteBatch.Draw(_pixelTex, t.Position, _pixelTex.Bounds, dimColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            _spriteBatch.Draw(_pixelTex, t.Position + t.Padding * t.Scale, _pixelTex.Bounds, paddingColor, 0f, Vector2.Zero,
                scale - 2 * t.Padding * t.Scale, SpriteEffects.None, 0f);
        }
    }
}
