using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.Entities
{
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Laser
    }

    public class Enemy : Entity
    {
        public EnemyType Type;

        private float _speed;
        private float _shootTimer = 2f;
        private int _direction = 1;

        public int Health = 1;
        public bool IsDead => Health <= 0;

        public Enemy(Vector2 startPos, EnemyType type)
        {
            position = startPos;
            size = new Vector2(32, 32);

            Type = type;

            switch (Type)
            {
                case EnemyType.Normal:
                    _speed = 2f;
                    Health = 1;
                    break;

                case EnemyType.Fast:
                    _speed = 4f;
                    Health = 1;
                    break;

                case EnemyType.Tank:
                    _speed = 1.5f;
                    Health = 3;
                    break;

                case EnemyType.Laser:
                    _speed = 2.5f;
                    Health = 2;
                    break;
            }
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
        }

        // =========================
        // SINGLE CLEAN UPDATE
        // =========================
        public virtual void Update(
            List<Rectangle> solids,
            Player player,
            List<Projectile> projectiles)
        {
            if (IsDead)
                return;

            // =========================
            // MOVEMENT
            // =========================
            position.X += _speed * _direction;

            Rectangle rect = Bounds;

            foreach (var tile in solids)
            {
                if (rect.Intersects(tile))
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

            rect = Bounds;

            foreach (var tile in solids)
            {
                if (rect.Intersects(tile))
                {
                    position.Y = tile.Top - size.Y;
                    velocity.Y = 0;
                    break;
                }
            }

            // =========================
            // LASER ATTACK
            // =========================
            if (Type == EnemyType.Laser)
            {
                _shootTimer -= 1f / 60f;

                if (_shootTimer <= 0f)
                {
                    _shootTimer = 2f;

                    Vector2 dir = player.position - position;

                    if (dir != Vector2.Zero)
                        dir.Normalize();

                    projectiles.Add(
                        new Projectile(position + new Vector2(16, 16), dir)
                    );
                }
            }
        }

        // =========================
        // DRAW
        // =========================
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead)
                return;

            Color color = Color.Red;

            switch (Type)
            {
                case EnemyType.Fast:
                    color = Color.Orange;
                    break;

                case EnemyType.Tank:
                    color = Color.DarkRed;
                    break;

                case EnemyType.Laser:
                    color = Color.Purple;
                    break;
            }

            spriteBatch.Draw(TextureManager.Pixel, Bounds, color);
        }

    }
}