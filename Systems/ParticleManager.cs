using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TheLostRobotStory.Core
{
    public class ParticleManager
    {

        private readonly List<Particle> _particles = new();







        // ==========================================
        // NORMAL EXPLOSION
        // ==========================================

        public void SpawnExplosion(
            Vector2 position,
            Color color)
        {

            // Main particles

            for (int i = 0; i < 25; i++)
            {

                float angle =
                    MathHelper.ToRadians(
                        Random.Shared.Next(360));



                float speed =
                    40f +
                    (float)Random.Shared.NextDouble()
                    * 80f;



                Vector2 velocity =
                    new Vector2(
                        (float)Math.Cos(angle),
                        (float)Math.Sin(angle))
                    *
                    speed;



                _particles.Add(
                    new Particle(
                        position,
                        velocity,
                        color));

            }





            // Small bright center particles

            for (int i = 0; i < 8; i++)
            {

                float angle =
                    MathHelper.ToRadians(
                        Random.Shared.Next(360));



                float speed =
                    60f +
                    (float)Random.Shared.NextDouble()
                    * 60f;



                Vector2 velocity =
                    new Vector2(
                        (float)Math.Cos(angle),
                        (float)Math.Sin(angle))
                    *
                    speed;



                _particles.Add(
                    new Particle(
                        position,
                        velocity,
                        Color.White));

            }

        }







        // ==========================================
        // ACID BUBBLE EFFECT
        // ==========================================

        public void SpawnBubble(
            Vector2 position,
            Color color)
        {

            float xOffset =
                (float)(Random.Shared.NextDouble() - 0.5f)
                *
                30f;



            Vector2 spawnPosition =
                position +
                new Vector2(
                    xOffset,
                    0);



            Vector2 velocity =
                new Vector2(
                    xOffset * 0.05f,
                    -30f -
                    (float)Random.Shared.NextDouble() * 40f);



            Particle bubble =
                new Particle(
                    spawnPosition,
                    velocity,
                    color);



            bubble.Size =
                3f +
                (float)Random.Shared.NextDouble() * 5f;



            bubble.MaxLife =
                1f;



            bubble.Life =
                bubble.MaxLife;



            _particles.Add(
                bubble);

        }








        // ==========================================
        // UPDATE
        // ==========================================

        public void Update(
            GameTime gameTime)
        {

            for (int i = _particles.Count - 1;
                 i >= 0;
                 i--)
            {

                _particles[i].Update(
                    gameTime);



                if (_particles[i].IsDead)
                {
                    _particles.RemoveAt(i);
                }

            }

        }







        // ==========================================
        // DRAW
        // ==========================================

        public void Draw(
            SpriteBatch spriteBatch,
            Texture2D pixel)
        {

            foreach (Particle particle in _particles)
            {

                particle.Draw(
                    spriteBatch,
                    pixel);

            }

        }

    }
}