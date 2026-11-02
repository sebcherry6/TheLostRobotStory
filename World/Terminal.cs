using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.World
{
    // Lore terminal ('T' in level files). Player presses E to read a log.
    public class Terminal
    {
        public Rectangle Bounds { get; }
        public string LogId { get; }

        public bool IsPlayerNear;
        public bool IsRead;

        private float _time;

        public Terminal(Vector2 tilePosition, string logId)
        {
            // 24x48, standing on the floor of its tile
            Bounds = new Rectangle(
                (int)tilePosition.X + 4,
                (int)tilePosition.Y - 16,
                24,
                48);

            LogId = logId;
        }

        public void Update(float dt)
        {
            _time += dt;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // terminal.png: 2 frames of 24x48 -> 0 = unread, 1 = read
            if (TextureManager.Terminal != null)
            {
                Rectangle source = Sprites.FrameRect(TextureManager.Terminal, IsRead ? 1 : 0, 24);

                if (IsPlayerNear)
                    Sprites.DrawOutline(spriteBatch, TextureManager.Terminal, Bounds, source, Color.White * 0.8f);

                spriteBatch.Draw(TextureManager.Terminal, Bounds, source, Color.White);
                return;
            }

            spriteBatch.Draw(TextureManager.Pixel, Bounds, new Color(45, 50, 62));

            Rectangle screen = new Rectangle(Bounds.X + 3, Bounds.Y + 4, Bounds.Width - 6, 16);

            // unread logs blink to attract the player
            float blink = 0.6f + 0.4f * (float)Math.Sin(_time * 4f);
            Color screenColor = IsRead ? Color.DarkCyan : Color.Orange * blink;

            spriteBatch.Draw(TextureManager.Pixel, screen, screenColor);

            if (IsPlayerNear)
            {
                Rectangle outline = Bounds;
                outline.Inflate(2, 2);
                spriteBatch.Draw(TextureManager.Pixel, outline, Color.White * 0.15f);
            }
        }
    }
}
