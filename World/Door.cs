using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

public class Door
{
    public Rectangle Bounds;
    public string DestinationLevel;

    public bool CanOpen;
    public bool IsActive = true;

    public bool IsPlayerNear;

    private float _glowTimer;
    private float _cooldownTimer;

    public Door(Vector2 position, string destinationLevel)
    {
        Bounds = new Rectangle((int)position.X, (int)position.Y, 64, 96);
        DestinationLevel = destinationLevel;
    }

    // =========================
    // UPDATE
    // =========================
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // glow animation
        if (IsPlayerNear)
            _glowTimer += dt * 6f;
        else
            _glowTimer = 0f;

        // cooldown after use
        if (!IsActive)
        {
            _cooldownTimer += dt;

            if (_cooldownTimer >= 1f)
            {
                IsActive = true;
                _cooldownTimer = 0f;
            }
        }
    }

    // =========================
    // CALL THIS FROM GAME1
    // =========================
    public void TryOpen()
    {
        if (!IsActive || !CanOpen)
            return;

        IsActive = false;
    }

    // =========================
    // DRAW (GLOW EFFECT)
    // =========================
    public void Draw(SpriteBatch spriteBatch)
    {
        float glow = 1f;

        if (IsPlayerNear)
        {
            glow = 0.6f + (float)System.Math.Sin(_glowTimer) * 0.4f;
        }

        Color color = Color.Purple * glow;

        if (!CanOpen)
            color = Color.DarkSlateBlue; // locked door look

        spriteBatch.Draw(TextureManager.Pixel, Bounds, color);
    }
}