using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Projectile
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public bool IsDead;

        public bool FromPlayer;

        private const float Speed = 10f;
        private const int Size = 8;
        private const int Damage = 1;

        public Rectangle Bounds =>
            new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                Size,
                Size);

        public Projectile(Vector2 startPosition, Vector2 direction, bool fromPlayer = true)
        {
            Position = startPosition;

            if (direction != Vector2.Zero)
                direction.Normalize();

            Velocity = direction * Speed;

            FromPlayer = fromPlayer;
        }

        public void Update(List<Rectangle> solids)
        {
            Position += Velocity;

            foreach (Rectangle tile in solids)
            {
                if (Bounds.Intersects(tile))
                {
                    IsDead = true;
                    return;
                }
            }
        }

        public bool HitEnemy(Enemy enemy)
        {
            if (!FromPlayer)
                return false;

            if (enemy.IsDead)
                return false;

            if (!Bounds.Intersects(enemy.Bounds))
                return false;

            enemy.TakeDamage(Damage);

            IsDead = true;

            return true;
        }

        public bool HitPlayer(Player player)
        {
            if (FromPlayer)
                return false;

            if (!Bounds.Intersects(player.Bounds))
                return false;

            player.Health--;

            IsDead = true;

            return true;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                FromPlayer ? Color.Yellow : Color.Red);
        }
    }
}