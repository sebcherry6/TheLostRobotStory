using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Boss : Enemy
    {
        public int MaxHealth = 60;
        public int Health;

        public bool IsDead => Health <= 0;

        // =========================
        // MOVEMENT
        // =========================
        private Vector2 _velocity;
        private float _moveSpeed = 180f;

        // =========================
        // ATTACK TIMERS
        // =========================
        private float _shootTimer = 1.2f;
        private float _dashTimer = 5f;
        private float _slamTimer = 4f;

        private bool _dashing;
        private Vector2 _dashVelocity;

        // =========================
        // DAMAGE COOLDOWN (IMPORTANT FIX)
        // =========================
        private float _hitCooldown;

        // =========================
        // GRAVITY PULL
        // =========================
        private float _gravityPullRadius = 350f;
        private float _pullStrength = 180f;

        public static float ArenaShrink = 0f;
        public static System.Action BossSlamEvent;

        public Boss(Vector2 startPos)
            : base(startPos, EnemyType.Laser)
        {
            position = startPos;
            size = new Vector2(96, 96);

            Health = MaxHealth; // IMPORTANT FIX
        }

        // =========================
        // DAMAGE (FIXED)
        // =========================
        public void TakeDamage(int dmg)
        {
            if (_hitCooldown > 0f)
                return;

            Health -= dmg;
            _hitCooldown = 0.15f; // prevents instant multi-hit melt
        }

        // =========================
        // UPDATE
        // =========================
        public void Update(
            List<Rectangle> solids,
            Player player,
            List<Projectile> projectiles,
            float dt)
        {
            if (IsDead)
                return;

            // cooldown tick
            if (_hitCooldown > 0f)
                _hitCooldown -= dt;

            // timers
            _shootTimer -= dt;
            _dashTimer -= dt;
            _slamTimer -= dt;

            ApplyGravityPull(player, dt);

            // =========================
            // DASH
            // =========================
            if (_dashTimer <= 0f && !_dashing)
            {
                _dashTimer = 6f;

                Vector2 dir = player.position - position;
                if (dir != Vector2.Zero)
                    dir.Normalize();

                _dashVelocity = dir * 600f;
                _dashing = true;
            }

            if (_dashing)
            {
                position += _dashVelocity * dt;
                _dashVelocity *= 0.92f;

                if (_dashVelocity.Length() < 40f)
                    _dashing = false;

                return;
            }

            // =========================
            // CHASE (HOVER)
            // =========================
            Vector2 toPlayer = player.position - position;

            if (toPlayer != Vector2.Zero)
                toPlayer.Normalize();

            position += toPlayer * _moveSpeed * dt;

            // =========================
            // SHOOT
            // =========================
            if (_shootTimer <= 0f)
            {
                _shootTimer = 1.0f;
                Fire(player, projectiles);
            }

            // =========================
            // SLAM EVENT
            // =========================
            if (_slamTimer <= 0f)
            {
                _slamTimer = 5f;
                BossSlamEvent?.Invoke();
                ArenaShrink += 10f;
            }
        }

        // =========================
        // GRAVITY PULL
        // =========================
        private void ApplyGravityPull(Player player, float dt)
        {
            Vector2 toBoss = position - player.position;
            float dist = toBoss.Length();

            if (dist < _gravityPullRadius && dist > 1f)
            {
                toBoss.Normalize();

                float force = _pullStrength / dist;

                player.position += toBoss * force * dt;
            }
        }

        // =========================
        // SHOOT
        // =========================
        private void Fire(Player player, List<Projectile> projectiles)
        {
            Vector2 dir = player.position - position;

            if (dir != Vector2.Zero)
                dir.Normalize();

            projectiles.Add(new Projectile(
                position + new Vector2(size.X / 2, size.Y / 2),
                dir,
                false
            ));
        }

        // =========================
        // DRAW (NO HP BAR HERE)
        // =========================
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead)
                return;

            float hp = (float)Health / MaxHealth;

            Color color =
                hp > 0.5f ? Color.Black :
                hp > 0.25f ? Color.DarkRed :
                Color.Purple;

            spriteBatch.Draw(TextureManager.Pixel, Bounds, color);
        }
    }
}