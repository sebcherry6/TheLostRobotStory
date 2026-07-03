using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

public class Door
{
    public Rectangle Bounds;
    public string DestinationLevel;
    public float GlowTimer;
    public bool IsPlayerNear;

    public bool IsActive = true;
    private float _cooldownTimer = 0f;

    public Door(Vector2 position, string destinationLevel)
    {
        Bounds = new Rectangle((int)position.X, (int)position.Y, 64, 96);
        DestinationLevel = destinationLevel;
    }

    public void Update(GameTime gameTime)
    {
        if (IsPlayerNear)
        {
            GlowTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 6f;
        }
        else
        {
            GlowTimer = 0f;
        }
        if (!IsActive)
        {
            _cooldownTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_cooldownTimer >= 1f)
            {
                IsActive = true;
                _cooldownTimer = 0f;
            }
        }
    }

    public void Trigger()
    {
        IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        float glow = 1f;

        if (IsPlayerNear)
        {
            glow = 0.5f + (float)System.Math.Sin(GlowTimer) * 0.5f;
        }

        Color color = Color.Purple * glow;

        spriteBatch.Draw(
            TextureManager.Pixel,
            Bounds,
            color);
    }
}