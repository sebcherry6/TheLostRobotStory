using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Player : Entity
    {
        public int EvolutionStage = 0;

        public int Health = 3;
        public Vector2 SpawnPoint;

        public int FacingDirection = 1;

        // movement
        private float _speed = 4f;
        private float _gravity = 0.5f;
        private float _jumpForce = -15f;

        private bool _isGrounded;
        private bool _doubleJumpUsed;

        // input
        private KeyboardState _keyboard;
        private KeyboardState _previousKeyboard;

        // attack
        public bool IsAttacking;
        private float _attackTimer;
        private float _attackCooldown = 0.25f;

        // invincibility
        private float _invincibleTimer;
        private float _invincibleDuration = 1f;

        public Player(Vector2 startPos)
        {
            position = startPos;
            SpawnPoint = startPos;
            size = new Vector2(32, 32);
        }

        public void Update(GameTime gameTime, List<Projectile> projectiles)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _keyboard = Keyboard.GetState();

            if (EvolutionStage >= 1)
                _speed = 5.5f;

            // =========================
            // HORIZONTAL MOVEMENT
            // =========================
            if (_keyboard.IsKeyDown(Keys.A))
            {
                position.X -= _speed;
                FacingDirection = -1;
            }

            if (_keyboard.IsKeyDown(Keys.D))
            {
                position.X += _speed;
                FacingDirection = 1;
            }

            // =========================
            // JUMP (FIXED ORDER)
            // =========================
            if (_keyboard.IsKeyDown(Keys.Space) &&
                _previousKeyboard.IsKeyUp(Keys.Space))
            {
                if (_isGrounded)
                {
                    velocity.Y = _jumpForce;
                    _doubleJumpUsed = false;
                }
                else if (EvolutionStage >= 1 && !_doubleJumpUsed)
                {
                    velocity.Y = _jumpForce;
                    _doubleJumpUsed = true;
                }
            }

            // =========================
            // GRAVITY
            // =========================
            velocity.Y += _gravity;
            position.Y += velocity.Y;

            // =========================
            // SHOOT (K) FIXED
            // =========================
            if (EvolutionStage >= 1 &&
                _keyboard.IsKeyDown(Keys.K) &&
                _previousKeyboard.IsKeyUp(Keys.K))
            {
                projectiles.Add(new Projectile(
                    position + new Vector2(size.X / 2, size.Y / 2),
                    new Vector2(FacingDirection, 0),
                    true
                ));
            }

            // =========================
            // ATTACK TIMER
            // =========================
            if (_attackTimer > 0)
            {
                _attackTimer -= dt;
                IsAttacking = true;
            }
            else
            {
                IsAttacking = false;
            }

            if (_keyboard.IsKeyDown(Keys.J) && _attackTimer <= 0)
                _attackTimer = _attackCooldown;

            // invincibility
            if (_invincibleTimer > 0)
                _invincibleTimer -= dt;

            _previousKeyboard = _keyboard;
        }

        // =========================
        // COLLISION (THIS FIXES JUMP)
        // =========================
        public void ApplyCollision(List<Rectangle> solids)
        {
            _isGrounded = false;

            Rectangle box = Bounds;

            foreach (var tile in solids)
            {
                if (box.Intersects(tile))
                {
                    Rectangle overlap = Rectangle.Intersect(box, tile);

                    if (overlap.Height < overlap.Width)
                    {
                        if (position.Y < tile.Y)
                        {
                            position.Y = tile.Top - size.Y;
                            velocity.Y = 0;
                            _isGrounded = true;
                            _doubleJumpUsed = false;
                        }
                        else
                        {
                            position.Y = tile.Bottom;
                            velocity.Y = 0;
                        }

                        box = Bounds;
                    }
                    else
                    {
                        if (position.X < tile.X)
                            position.X = tile.X - size.X;
                        else
                            position.X = tile.Right;

                        box = Bounds;
                    }
                }
            }
        }

        public Rectangle AttackHitbox =>
            IsAttacking
                ? new Rectangle(
                    (int)(position.X + (FacingDirection == 1 ? size.X : -20)),
                    (int)position.Y,
                    (int)size.X,
                    (int)size.Y)
                : Rectangle.Empty;

        public void CheckEnemyCollision(List<Enemy> enemies)
        {
            if (_invincibleTimer > 0)
                return;

            foreach (var enemy in enemies)
            {
                if (Bounds.Intersects(enemy.Bounds))
                {
                    Health--;
                    _invincibleTimer = _invincibleDuration;

                    position.X += FacingDirection * -10;
                    position.Y -= 5;
                    break;
                }
            }
        }

        public void CheckDeath()
        {
            if (Health <= 0)
            {
                position = SpawnPoint;
                velocity = Vector2.Zero;
                Health = 3;
                _doubleJumpUsed = false;
                _invincibleTimer = 1f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Color c = Color.White;

            if (IsAttacking) c = Color.Orange;
            if (_invincibleTimer > 0) c = Color.LightBlue;

            spriteBatch.Draw(TextureManager.Pixel, Bounds, c);
        }
    }
}