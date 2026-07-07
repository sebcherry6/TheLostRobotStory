using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class EvolutionCore
    {
        public Vector2 Position;

        public bool Collected;

        public int TargetEvolutionStage;

        private float _time;

        private const int Size = 32;

        public Rectangle Bounds =>
            new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                Size,
                Size);

        public EvolutionCore(Vector2 position, int targetStage)
        {
            Position = position;
            TargetEvolutionStage = targetStage;
            Collected = false;
        }

        // ==========================================
        // UPDATE
        // ==========================================
        public void Update(GameTime gameTime)
        {
            if (Collected)
                return;

            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        // ==========================================
        // COLLECT
        // ==========================================
        public void Collect(Player player)
        {
            if (Collected)
                return;

            Collected = true;

            if (player.EvolutionStage < TargetEvolutionStage)
            {
                player.EvolutionStage = TargetEvolutionStage;

                // Restore the player when evolving
                player.Health = 3;

                // Future additions:
                // Play evolution sound
                // Spawn particles
                // Display "Evolution Complete!"
            }
        }

        // ==========================================
        // DRAW
        // ==========================================
        public void Draw(SpriteBatch spriteBatch)
        {
            if (Collected)
                return;

            float floatOffset =
                (float)Math.Sin(_time * 3f) * 5f;

            float pulse =
                0.6f +
                (float)Math.Sin(_time * 6f) * 0.25f;

            Rectangle glow = new Rectangle(
                (int)Position.X - 6,
                (int)(Position.Y + floatOffset) - 6,
                Size + 12,
                Size + 12);

            Rectangle core = new Rectangle(
                (int)Position.X,
                (int)(Position.Y + floatOffset),
                Size,
                Size);

            // Glow
            spriteBatch.Draw(
                TextureManager.Pixel,
                glow,
                Color.Cyan * pulse);

            // Core
            spriteBatch.Draw(
                TextureManager.Pixel,
                core,
                Color.DeepSkyBlue);

            // Bright center
            Rectangle center = new Rectangle(
                core.X + 8,
                core.Y + 8,
                16,
                16);

            spriteBatch.Draw(
                TextureManager.Pixel,
                center,
                Color.White);
        }
    }
}