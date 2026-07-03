using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

public class HUD
{
    public void Draw(SpriteBatch spriteBatch, int health)
    {
        for (int i = 0; i < health; i++)
        {
            spriteBatch.Draw(
                TextureManager.Pixel,
                new Rectangle(20 + (i * 20), 20, 15, 15),
                Color.Red
            );
        }
    }
}