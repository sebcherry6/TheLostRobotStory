using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TheLostRobotStory.Entities;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Player : Entity
    {
        private float _speed = 4f;
        private float _gravity = 0.5f;
        private float _jumpForce = -10f;

        private KeyboardState _keyboard;
        private KeyboardState _previousKeyboard;

        public int Health = 3;

        // =========================
        // SPAWN SYSTEM
        // =========================
        public Vector2 SpawnPoint;

        // =========================
        // INVINCIBILITY FRAMES
        // =========================
        private float _invincibleTimer = 0f;
        private float _invincibleDuration = 1f;

        // =========================
        // COMBAT
        // =========================
        public bool IsAttacking;
        private float _attackTimer;
        private float _attackCooldown = 0.25f;

        public Player(Vector2 startPos)
        {
            position = startPos;
            SpawnPoint = startPos;
            size = new Vector2(32, 32);
        }

        public override void Update(GameTime gameTime)
        {
            _keyboard = Keyboard.GetState();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // =========================
            // INVINCIBILITY TIMER
            // =========================
            if (_invincibleTimer > 0)
                _invincibleTimer -= dt;

            // =========================
            // MOVEMENT
            // =========================
            if (_keyboard.IsKeyDown(Keys.A))
                position.X -= _speed;

            if (_keyboard.IsKeyDown(Keys.D))
                position.X += _speed;

            // =========================
            // JUMP
            // =========================
            if (_keyboard.IsKeyDown(Keys.Space) &&
                _previousKeyboard.IsKeyUp(Keys.Space))
            {
                velocity.Y = _jumpForce;
            }

            // =========================
            // GRAVITY
            // =========================
            velocity.Y += _gravity;
            position.Y += velocity.Y;

            // =========================
            // ATTACK SYSTEM
            // =========================
            if (_attackTimer > 0)
            {
                _attackTimer -= dt;
            }
            else
            {
                IsAttacking = false;
            }

            if (_keyboard.IsKeyDown(Keys.J) &&
                _attackTimer <= 0)
            {
                IsAttacking = true;
                _attackTimer = _attackCooldown;
            }

            _previousKeyboard = _keyboard;
        }

        // =========================
        // ATTACK HITBOX
        // =========================
        public Rectangle AttackHitbox
        {
            get
            {
                if (!IsAttacking)
                    return Rectangle.Empty;

                return new Rectangle(
                    (int)(position.X + size.X),
                    (int)position.Y,
                    (int)size.X,
                    (int)size.Y
                );
            }
        }

        // =========================
        // WORLD COLLISION
        // =========================
        public void ApplyCollision(List<Rectangle> solids)
        {
            Rectangle playerRect = Bounds;

            foreach (var tile in solids)
            {
                if (playerRect.Intersects(tile))
                {
                    if (velocity.Y > 0)
                    {
                        position.Y = tile.Top - size.Y;
                        velocity.Y = 0;
                    }
                }
            }
        }

        // =========================
        // ENEMY COLLISION (FIXED)
        // =========================
        public void CheckEnemyCollision(List<Enemy> enemies)
        {
            if (_invincibleTimer > 0)
                return;

            Rectangle playerRect = Bounds;

            foreach (var enemy in enemies)
            {
                if (playerRect.Intersects(enemy.Bounds))
                {
                    Health--;
                    _invincibleTimer = _invincibleDuration;
                    break;
                }
            }
        }

        // =========================
        // DEATH / RESPAWN (FIXED)
        // =========================
        public void CheckDeath()
        {
            if (Health <= 0)
            {
                position = SpawnPoint;
                velocity = Vector2.Zero;
                Health = 3;

                _invincibleTimer = 1f; // prevents instant re-hit on spawn
            }
        }

        // =========================
        // DRAW
        // =========================
        public override void Draw(SpriteBatch spriteBatch)
        {
            Color color = Color.White;

            if (IsAttacking)
                color = Color.LightYellow;

            if (_invincibleTimer > 0)
                color = Color.LightBlue; // visual feedback for damage

            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color
            );
        }
    }
}