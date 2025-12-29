/* * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  Author: TakoPuck (2025)                            *
 *  This code is open and free to use for any purpose. *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using CoolCustomText.Source;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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

            string text = "Hello stranger, are you <fx 2,0,0,1,0>good</fx> <fx 0,1,0,0,0>?</fx>\n<fx 1,1,0,0,0>*************************************</fx><fx 6,0,1,0,0>This line is scared</fx> <fx 6,0,0,0,1>></fx> <fx 7,0,0,0,0>0123456789</fx> <fx 6,0,0,0,2><</fx>";
            Vector2 textDim = new(284f, 60f); // The dimension is multiplied by 4 later.
            Vector2 position = new(50f);

            _customText = new(this, "PixellariFont", text, position, textDim)
            {
                Scale = new(4f), // Scale the dimension. This is useful if you're working with pixel art UI to match the pixel per unit and have the dimension (& padding) based on pixel art size.
                Color = new(255, 244, 196),
                Padding = new(5f, 0f),
                ShadowColor = new(128, 85, 111), // By default it's Color.Transparent which disable it.
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


            _infoCustomText = new(this, "SmallPixellariFont",
                "The gray box represents the dimension of the custom text but\nthe input text is rendered into the green box because we have set a padding.\n" +
                "Overflow is allowed here, but by default it isn't and you have to called NextPage or NextStartingLine to draw the overflowing text. " +
                "\nNewlines works\nperfectly too           (consecutives spaces        too).\n" +
                "Finally to give a <fx 5,1,0,1,0>special effect</fx> to your text, use the fx tag by setting the profile of the specific effect " +
                "(ignore effect with zero), the syntax is:\n" +
                "<fx Color Palette, Wave profile, Shake pro., Hang pro., Side step pro.>text</fx>\n" +
                "\nSee README.md to know how to create new profiles.",
                position: new(40f, 310f), dimension: new(1200f, 92f), padding: new(0f, 10f), allowOverflow: true);
            _infoCustomText.Refresh();

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
