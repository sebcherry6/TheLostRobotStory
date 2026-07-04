using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Boss : Enemy
    {
        private float _moveSpeed = 2.5f;

        private float _shootTimer = 0f;
        private float _shootCooldown = 2f;

        private float _chargeTimer = 0f;
        private float _chargeCooldown = 5f;

        private bool _isCharging = false;
        private Vector2 _chargeDirection;

        public Boss(Vector2 startPos)
            : base(startPos, EnemyType.Tank)
        {
            position = startPos;

            size = new Vector2(96, 96);

            Health = 20;
        }

        public override void Update(
            List<Rectangle> solids,
            Player player,
            List<Projectile> projectiles)
        {
            if (IsDead)
                return;

            float dt = 1f / 60f;

            // =========================
            // TIMERS
            // =========================
            _shootTimer += dt;
            _chargeTimer += dt;

            // =========================
            // START CHARGE
            // =========================
            if (_chargeTimer >= _chargeCooldown)
            {
                _chargeTimer = 0f;

                _isCharging = true;

                _chargeDirection = player.position - position;

                if (_chargeDirection != Vector2.Zero)
                    _chargeDirection.Normalize();
            }

            // =========================
            // CHARGE MOVEMENT
            // =========================
            if (_isCharging)
            {
                position += _chargeDirection * 8f;

                if (Vector2.Distance(position, player.position) < 40)
                    _isCharging = false;
            }
            else
            {
                Vector2 direction = player.position - position;

                if (direction != Vector2.Zero)
                    direction.Normalize();

                position += direction * _moveSpeed;
            }

            // =========================
            // SHOOT SPREAD
            // =========================
            if (_shootTimer >= _shootCooldown)
            {
                _shootTimer = 0f;

                ShootSpread(player, projectiles);
            }
        }

        private void ShootSpread(Player player, List<Projectile> projectiles)
        {
            Vector2 direction = player.position - position;

            if (direction == Vector2.Zero)
                return;

            direction.Normalize();

            Vector2 perpendicular = new Vector2(
                -direction.Y,
                 direction.X);

            projectiles.Add(new Projectile(
                position + size / 2,
                direction));

            projectiles.Add(new Projectile(
                position + size / 2,
                Vector2.Normalize(direction + perpendicular * 0.25f)));

            projectiles.Add(new Projectile(
                position + size / 2,
                Vector2.Normalize(direction - perpendicular * 0.25f)));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead)
                return;

            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                Color.DarkMagenta);

            // =========================
            // HEALTH BAR
            // =========================

            Rectangle back = new Rectangle(
                (int)position.X,
                (int)position.Y - 12,
                (int)size.X,
                8);

            spriteBatch.Draw(
                TextureManager.Pixel,
                back,
                Color.DarkRed);

            Rectangle health = new Rectangle(
                (int)position.X,
                (int)position.Y - 12,
                (int)(size.X * (Health / 20f)),
                8);

            spriteBatch.Draw(
                TextureManager.Pixel,
                health,
                Color.LimeGreen);
        }
    }
}