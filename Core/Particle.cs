using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TheLostRobotStory.Core
{
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public Color Color;
        public float Size;

        public Particle(Vector2 position, Vector2 velocity, Color color)
        {
            Position = position;
            Velocity = velocity;
            Color = color;

            MaxLife = 0.6f + (float)Random.Shared.NextDouble() * 0.4f;
            Life = MaxLife;

            Size = 4f + (float)Random.Shared.NextDouble() * 4f;
        }

        public bool IsDead => Life <= 0;

        public void Update(GameTime gameTime)
        {
            Life -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            Position += Velocity;

            Velocity *= 0.95f; // friction
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            float alpha = Life / MaxLife;

            spriteBatch.Draw(
                pixel,
                Position,
                null,
                Color * alpha,
                0f,
                Vector2.Zero,
                Size,
                SpriteEffects.None,
                0f);
        }
    }
}