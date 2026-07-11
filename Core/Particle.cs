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

        private float _gravity;

        private float _rotation;

        private float _rotationSpeed;



        public Particle(
            Vector2 position,
            Vector2 velocity,
            Color color,
            float sizeMultiplier = 1f)
        {

            Position = position;

            Velocity = velocity;

            Color = color;



            MaxLife =
                0.5f +
                (float)Random.Shared.NextDouble() * 0.8f;


            Life = MaxLife;



            Size =
                (3f +
                (float)Random.Shared.NextDouble() * 5f)
                *
                sizeMultiplier;



            _gravity =
                80f +
                (float)Random.Shared.NextDouble() * 120f;



            _rotation =
                (float)Random.Shared.NextDouble()
                * MathHelper.TwoPi;



            _rotationSpeed =
                ((float)Random.Shared.NextDouble() - 0.5f)
                * 5f;

        }







        public bool IsDead =>
            Life <= 0;








        public void Update(
            GameTime gameTime)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            Life -= dt;



            Velocity.Y += _gravity * dt;



            Position += Velocity * dt;



            Velocity *= 0.96f;



            _rotation +=
                _rotationSpeed * dt;

        }








        public void Draw(
            SpriteBatch spriteBatch,
            Texture2D pixel)
        {

            float alpha =
                MathHelper.Clamp(
                    Life / MaxLife,
                    0f,
                    1f);



            spriteBatch.Draw(
                pixel,
                Position,
                null,
                Color * alpha,
                _rotation,
                new Vector2(
                    0.5f,
                    0.5f),
                Size,
                SpriteEffects.None,
                0f);

        }

    }
}