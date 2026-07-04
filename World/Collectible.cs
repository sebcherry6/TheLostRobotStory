using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.World
{
    public class Collectible
    {
        public Rectangle Bounds;
        public bool IsCollected;

        public Collectible(Vector2 pos)
        {
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, 16, 16);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsCollected) return;

            spriteBatch.Draw(TextureManager.Pixel, Bounds, Color.Gold);
        }
    }
}