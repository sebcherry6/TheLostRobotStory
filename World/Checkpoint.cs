using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

public class Checkpoint
{
    public Rectangle Bounds;

    public bool Activated;

    public Checkpoint(Vector2 position)
    {
        Bounds = new Rectangle(
            (int)position.X,
            (int)position.Y,
            32,
            64);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            TextureManager.Pixel,
            Bounds,
            Activated ? Color.LimeGreen : Color.Green);
    }
}