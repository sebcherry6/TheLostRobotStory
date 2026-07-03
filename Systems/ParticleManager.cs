using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TheLostRobotStory.Core
{
    public class ParticleManager
    {
        private List<Particle> _particles = new();

        public void SpawnExplosion(Vector2 position, Color color)
        {
            for (int i = 0; i < 15; i++)
            {
                float angle = MathHelper.ToRadians(Random.Shared.Next(360));
                float speed = (float)Random.Shared.NextDouble() * 4f;

                Vector2 velocity = new Vector2(
                    (float)System.Math.Cos(angle),
                    (float)System.Math.Sin(angle)) * speed;

                _particles.Add(new Particle(position, velocity, color));
            }
        }

        public void Update(GameTime gameTime)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update(gameTime);

                if (_particles[i].IsDead)
                    _particles.RemoveAt(i);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            foreach (var p in _particles)
                p.Draw(spriteBatch, pixel);
        }
    }
}