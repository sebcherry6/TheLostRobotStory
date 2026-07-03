using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.Entities
{
    public class Enemy : Entity
    {
        private float _speed = 2f;
        private int _direction = 1;

        public int Health = 1;
        public bool IsDead => Health <= 0;

        public Enemy(Vector2 startPos)
        {
            position = startPos;
            size = new Vector2(32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
        }

        // =========================
        // MAIN UPDATE (CALLED FROM GAME1)
        // =========================
        public void Update(List<Rectangle> solids)
        {
            if (IsDead)
                return;

            // =========================
            // HORIZONTAL MOVEMENT
            // =========================
            position.X += _speed * _direction;

            Rectangle enemyRect = Bounds;

            foreach (var tile in solids)
            {
                if (enemyRect.Intersects(tile))
                {
                    _direction *= -1;
                    position.X += _speed * _direction;
                    break;
                }
            }

            // =========================
            // GRAVITY
            // =========================
            velocity.Y += 0.5f;
            position.Y += velocity.Y;

            enemyRect = Bounds;

            foreach (var tile in solids)
            {
                if (enemyRect.Intersects(tile))
                {
                    position.Y = tile.Top - size.Y;
                    velocity.Y = 0;
                    break;
                }
            }
        }

        // =========================
        // DRAW (NO OVERRIDE ISSUES)
        // =========================
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead)
                return;

            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                Color.Red
            );
        }
    }
}