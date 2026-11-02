using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.Entities
{
    public class Checkpoint
    {
        // =========================
        // PROPERTIES
        // =========================

        public Rectangle Bounds { get; private set; }

        public bool Activated { get; private set; }

        // Raised once per checkpoint (GameplayScene autosaves on it)
        public static event Action CheckpointActivated;


        public Checkpoint(Vector2 position)
        {
            Bounds = new Rectangle(
                (int)position.X,
                (int)position.Y,
                32,
                64);
        }


        // =========================
        // ACTIVATE
        // =========================

        public void Activate(Player player)
        {
            if (Activated)
                return;

            Activated = true;

            // Save player's respawn location
            player.SpawnPoint = new Vector2(
                Bounds.Center.X - player.size.X / 2,
                Bounds.Bottom - player.size.Y);

            CheckpointActivated?.Invoke();
        }


        // =========================
        // DRAW
        // =========================

        public void Draw(SpriteBatch spriteBatch)
        {
            // checkpoint.png: 2 frames of 32x64 -> 0 = inactive, 1 = active
            if (TextureManager.Checkpoint != null)
            {
                Sprites.DrawFrame(spriteBatch, TextureManager.Checkpoint, Bounds, Activated ? 1 : 0, Color.White, 32);
                return;
            }

            Color color = Activated
                ? Color.LimeGreen
                : Color.ForestGreen;

            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

            // Small glowing top
            Rectangle light = new Rectangle(
                Bounds.X + 8,
                Bounds.Y + 4,
                16,
                8);

            spriteBatch.Draw(
                TextureManager.Pixel,
                light,
                Activated ? Color.Yellow : Color.DarkOliveGreen);
        }
    }
}