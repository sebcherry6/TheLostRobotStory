using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Projectile
    {
        public Vector2 position;
        public Vector2 velocity;

        public Rectangle Bounds =>
            new Rectangle((int)position.X, (int)position.Y, 8, 8);

        public bool IsDead;

        private float _speed = 8f;
        private int _damage = 1;

        // OPTIONAL: for enemy/player differentiation later
        public bool FromPlayer;

        public Projectile(Vector2 startPosition, Vector2 direction, bool fromPlayer = true)
        {
            position = startPosition;
            velocity = Vector2.Normalize(direction) * _speed;
            FromPlayer = fromPlayer;
        }

        public void Update(List<Rectangle> solids)
        {
            position += velocity;

            // =========================
            // TILE COLLISION
            // =========================
            Rectangle box = Bounds;

            foreach (var tile in solids)
            {
                if (box.Intersects(tile))
                {
                    IsDead = true;
                    return;
                }
            }
        }

        // =========================
        // ENEMY HIT CHECK (PLAYER PROJECTILE)
        // =========================
        public bool HitsEnemy(Enemy enemy)
        {
            if (FromPlayer && Bounds.Intersects(enemy.Bounds))
            {
                enemy.TakeDamage(_damage);
                IsDead = true;
                return true;
            }

            return false;
        }

        // =========================
        // PLAYER HIT CHECK (ENEMY PROJECTILE)
        // =========================
        public bool HitsPlayer(Player player)
        {
            if (!FromPlayer && Bounds.Intersects(player.Bounds))
            {
                player.Health--;
                IsDead = true;
                return true;
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                FromPlayer ? Color.Yellow : Color.Red
            );
        }
    }
}