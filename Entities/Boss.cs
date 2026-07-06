using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Boss : Enemy
    {
        public int MaxHealth = 30;
        public int Health;

        public bool IsDead => Health <= 0;

        private int _direction = 1;

        private float _gravity = 0.5f;

        // =========================
        // PHASE / AI TIMERS
        // =========================
        private float _shootTimer = 2f;
        private float _chargeTimer = 5f;
        private float _stompTimer = 4f;

        private bool _isCharging;
        private Vector2 _chargeVelocity;

        public Boss(Vector2 startPos)
            : base(startPos, EnemyType.Tank) // or Boss type if you add one
        {
            size = new Vector2(64, 64);
            Health = 20;
        }

        // =========================
        // PHASE LOGIC
        // =========================
        private int Phase
        {
            get
            {
                float hpPercent = (float)Health / MaxHealth;

                if (hpPercent > 0.66f)
                    return 1;

                if (hpPercent > 0.33f)
                    return 2;

                return 3;
            }
        }

        public void TakeDamage(int dmg)
        {
            Health -= dmg;
        }

        // =========================
        // MAIN UPDATE
        // =========================
        public void Update(List<Rectangle> solids, Player player, List<Projectile> projectiles)
        {
            if (IsDead)
                return;

            float speed = GetSpeed();

            // =========================
            // CHARGE ATTACK (PHASE 2+)
            // =========================
            if (Phase >= 2)
            {
                _chargeTimer -= 1f / 60f;

                if (!_isCharging && _chargeTimer <= 0)
                {
                    _isCharging = true;

                    Vector2 dir = player.position - position;
                    if (dir != Vector2.Zero)
                        dir.Normalize();

                    _chargeVelocity = dir * 10f;
                }

                if (_isCharging)
                {
                    position += _chargeVelocity;

                    ResolveHorizontal(solids);

                    _chargeVelocity *= 0.95f;

                    if (_chargeVelocity.Length() < 1f)
                    {
                        _isCharging = false;
                        _chargeTimer = 4f;
                    }

                    return; // skip normal AI while charging
                }
            }

            // =========================
            // NORMAL MOVEMENT
            // =========================
            position.X += speed * _direction;

            ResolveHorizontal(solids);

            // =========================
            // GRAVITY
            // =========================
            velocity.Y += _gravity;
            position.Y += velocity.Y;

            ResolveVertical(solids);

            // =========================
            // SHOOTING (ALL PHASES)
            // =========================
            _shootTimer -= 1f / 60f;

            if (_shootTimer <= 0)
            {
                _shootTimer = GetShootRate();

                FireProjectile(player, projectiles);
            }

            // =========================
            // STOMP ATTACK (PHASE 3)
            // =========================
            if (Phase >= 3)
            {
                _stompTimer -= 1f / 60f;

                if (_stompTimer <= 0)
                {
                    _stompTimer = 3.5f;

                    velocity.Y = 10f; // big slam down

                    // NOTE: you can hook camera shake from Game1 when detecting stomp
                }
            }
        }

        // =========================
        // HELPERS
        // =========================
        private float GetSpeed()
        {
            switch (Phase)
            {
                case 1: return 2.5f;
                case 2: return 3.5f;
                case 3: return 4.5f;
                default: return 2.5f;
            }
        }

        private float GetShootRate()
        {
            switch (Phase)
            {
                case 1: return 2.0f;
                case 2: return 1.2f;
                case 3: return 0.6f;
                default: return 2.0f;
            }
        }

        private void FireProjectile(Player player, List<Projectile> projectiles)
        {
            Vector2 dir = player.position - position;

            if (dir != Vector2.Zero)
                dir.Normalize();

            // phase-based spread
            if (Phase == 1)
            {
                projectiles.Add(new Projectile(
                    position + new Vector2(size.X / 2, size.Y / 2),
                    dir,
                    false));
            }
            else if (Phase == 2)
            {
                projectiles.Add(new Projectile(position, dir, false));
                projectiles.Add(new Projectile(position, new Vector2(dir.X, dir.Y + 0.2f), false));
            }
            else
            {
                // phase 3 bullet hell
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 spread = new Vector2(dir.X + i * 0.2f, dir.Y);
                    projectiles.Add(new Projectile(position, spread, false));
                }
            }
        }

        // =========================
        // COLLISION
        // =========================
        private void ResolveHorizontal(List<Rectangle> solids)
        {
            Rectangle box = Bounds;

            foreach (var tile in solids)
            {
                if (!box.Intersects(tile))
                    continue;

                _direction *= -1;

                if (position.X < tile.X)
                    position.X = tile.Left - size.X;
                else
                    position.X = tile.Right;

                break;
            }
        }

        private void ResolveVertical(List<Rectangle> solids)
        {
            Rectangle box = Bounds;

            foreach (var tile in solids)
            {
                if (!box.Intersects(tile))
                    continue;

                if (velocity.Y > 0)
                    position.Y = tile.Top - size.Y;
                else
                    position.Y = tile.Bottom;

                velocity.Y = 0;
                break;
            }
        }

        // =========================
        // DRAW
        // =========================
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead)
                return;

            Color color = Color.Black;

            if (Phase == 2)
                color = Color.DarkRed;
            else if (Phase == 3)
                color = Color.Purple;

            spriteBatch.Draw(TextureManager.Pixel, Bounds, color);
        }
    }
}