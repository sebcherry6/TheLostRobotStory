using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostRobotStory.Core
{
    // One convention for every texture in the game:
    // a PNG is either a single image, or a HORIZONTAL STRIP of equally sized
    // frames. The caller passes the frame width; the frame count is
    // texture.Width / frameWidth (optionally limited with maxFrames).
    public static class Sprites
    {
        // Shared animation clock (advanced by GameplayScene, frozen while paused)
        public static float Time { get; private set; }

        public static void Update(float dt)
        {
            Time += dt;
        }

        public static int FrameCount(Texture2D texture, int frameWidth)
        {
            if (texture == null)
                return 0;

            int width = frameWidth <= 0 ? texture.Width : Math.Min(frameWidth, texture.Width);
            return Math.Max(1, texture.Width / width);
        }

        // Source rectangle of one frame
        public static Rectangle FrameRect(Texture2D texture, int frame, int frameWidth = 0, int frameHeight = 0, int row = 0)
        {
            int width = frameWidth <= 0 ? texture.Width : Math.Min(frameWidth, texture.Width);
            int height = frameHeight <= 0 ? texture.Height : Math.Min(frameHeight, texture.Height);
            int count = Math.Max(1, texture.Width / width);

            frame = Math.Clamp(frame, 0, count - 1);

            return new Rectangle(frame * width, row * height, width, height);
        }

        // Which frame a looping animation shows right now (same maths as Draw)
        public static int AnimFrame(Texture2D texture, int frameWidth, float fps, float timeOffset = 0f, int maxFrames = 0)
        {
            int count = FrameCount(texture, frameWidth);

            if (maxFrames > 0)
                count = Math.Min(count, maxFrames);

            return count > 1 ? (int)(Math.Abs(Time + timeOffset) * fps) % count : 0;
        }

        // Draw one specific frame (e.g. door: 0 = sealed, 1 = unlocked)
        public static void DrawFrame(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Rectangle destination,
            int frame,
            Color color,
            int frameWidth = 0,
            SpriteEffects effects = SpriteEffects.None)
        {
            if (texture == null)
                return;

            spriteBatch.Draw(texture, destination, FrameRect(texture, frame, frameWidth), color, 0f, Vector2.Zero, effects, 0f);
        }

        // Draw a looping animation (single images just draw as-is)
        public static void Draw(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Rectangle destination,
            Color color,
            int frameWidth = 0,
            float fps = 8f,
            SpriteEffects effects = SpriteEffects.None,
            float timeOffset = 0f,
            int maxFrames = 0)
        {
            if (texture == null)
                return;

            int frame = AnimFrame(texture, frameWidth, fps, timeOffset, maxFrames);
            DrawFrame(spriteBatch, texture, destination, frame, color, frameWidth, effects);
        }

        // Stable pseudo-random number per grid cell (for tile variations)
        public static int CellHash(int x, int y)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663;
                return h & int.MaxValue;
            }
        }

        // =====================================================
        // OUTLINES THAT FOLLOW THE PIXELS
        // A white copy of the texture (same alpha) is drawn around the
        // sprite, so highlights hug the shape instead of making a box.
        // =====================================================

        private static readonly Dictionary<Texture2D, Texture2D> _silhouettes = new();

        public static Texture2D Silhouette(Texture2D texture)
        {
            if (_silhouettes.TryGetValue(texture, out Texture2D cached))
                return cached;

            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                byte a = data[i].A;
                data[i] = new Color(a, a, a, a); // premultiplied white
            }

            Texture2D white = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
            white.SetData(data);

            _silhouettes[texture] = white;
            return white;
        }

        // Call BEFORE drawing the sprite itself
        public static void DrawOutline(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Rectangle destination,
            Rectangle source,
            Color color,
            SpriteEffects effects = SpriteEffects.None,
            int thickness = 1)
        {
            if (texture == null)
                return;

            Texture2D white = Silhouette(texture);

            for (int t = 1; t <= thickness; t++)
            {
                Color c = color * (t == 1 ? 1f : 0.45f);

                DrawShifted(spriteBatch, white, destination, source, c, effects, -t, 0);
                DrawShifted(spriteBatch, white, destination, source, c, effects, t, 0);
                DrawShifted(spriteBatch, white, destination, source, c, effects, 0, -t);
                DrawShifted(spriteBatch, white, destination, source, c, effects, 0, t);
            }
        }

        private static void DrawShifted(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, Rectangle source, Color color, SpriteEffects effects, int dx, int dy)
        {
            destination.Offset(dx, dy);
            spriteBatch.Draw(texture, destination, source, color, 0f, Vector2.Zero, effects, 0f);
        }
    }
}
