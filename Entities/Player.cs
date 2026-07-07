using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Player : Entity
    {

        // =====================================================
        // EVOLUTION
        // =====================================================

        public int EvolutionStage = 0;


        public bool HasShownShootMessage = false;

        public bool HasShownDoubleJumpMessage = false;



        // =====================================================
        // HEALTH
        // =====================================================

        public int MaxHealth = 3;

        public int Health = 3;



        // =====================================================
        // CHECKPOINT
        // =====================================================

        public Vector2 SpawnPoint;


        private int _checkpointEvolution;

        private int _checkpointMaxHealth;

        private int _checkpointHealth;



        // =====================================================
        // MOVEMENT
        // =====================================================

        public int FacingDirection = 1;


        private float _speed = 220f;

        private float _gravity = 1800f;

        private float _jumpForce = -950f;


        private float _maxFallSpeed = 900f;



        private bool _grounded;

        private bool _doubleJumpUsed;



        // Coyote jump

        private float _coyoteTimer;

        private float _coyoteTime = 0.15f;



        // Jump buffering

        private float _jumpBufferTimer;

        private float _jumpBufferTime = 0.15f;



        // =====================================================
        // ATTACK
        // =====================================================

        public bool IsAttacking;


        private float _attackTimer;

        private float _attackCooldown = 0.25f;



        // =====================================================
        // DAMAGE
        // =====================================================

        private float _invincibleTimer;

        private float _invincibleDuration = 1f;



        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Player(Vector2 startPos)
        {

            position = startPos;


            SpawnPoint = startPos;


            size = new Vector2(32, 32);



            SaveCheckpoint();

        }



        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime,
            KeyboardState keyboard,
            KeyboardState previousKeyboard,
            List<Projectile> projectiles)
        {

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;



            HandleMovement(
                keyboard, dt);



            HandleJump(
                keyboard,
                previousKeyboard,
                dt);



            ApplyGravity(dt);



            HandleShooting(
                keyboard,
                previousKeyboard,
                projectiles);



            HandleAttack(
                keyboard,
                previousKeyboard,
                dt);



            if (_invincibleTimer > 0)
            {
                _invincibleTimer -= dt;
            }

        }



        // =====================================================
        // MOVEMENT
        // =====================================================

        private void HandleMovement(
    KeyboardState keyboard,
    float dt)
        {
            float speed = GetSpeed();

            velocity.X = 0;


            if (keyboard.IsKeyDown(Keys.A))
            {
                velocity.X = -speed;

                FacingDirection = -1;
            }


            if (keyboard.IsKeyDown(Keys.D))
            {
                velocity.X = speed;

                FacingDirection = 1;
            }



            // APPLY MOVEMENT
            position.X += velocity.X * dt;
        }
        // =====================================================
        // JUMP SYSTEM
        // =====================================================

        private void HandleJump(
            KeyboardState keyboard,
            KeyboardState previousKeyboard,
            float dt)
        {


            // Update coyote timer

            if (_grounded)
            {
                _coyoteTimer = _coyoteTime;
            }
            else
            {
                _coyoteTimer -= dt;
            }



            // Jump button pressed

            if (keyboard.IsKeyDown(Keys.Space)
                &&
               previousKeyboard.IsKeyUp(Keys.Space))
            {
                _jumpBufferTimer =
                    _jumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= dt;
            }




            // Try normal jump

            if (_jumpBufferTimer > 0)
            {

                if (_coyoteTimer > 0)
                {

                    velocity.Y =
                        _jumpForce;


                    _grounded = false;


                    _jumpBufferTimer = 0;


                    _coyoteTimer = 0;

                }


                // Double jump

                else if (EvolutionStage >= 2 &&
                        !_doubleJumpUsed)
                {

                    velocity.Y =
                        _jumpForce;


                    _doubleJumpUsed = true;


                    _jumpBufferTimer = 0;

                }

            }

        }





        // =====================================================
        // GRAVITY
        // =====================================================

        private void ApplyGravity(float dt)
        {

            velocity.Y +=
                _gravity * dt;



            if (velocity.Y > _maxFallSpeed)
            {
                velocity.Y =
                    _maxFallSpeed;
            }



            position.Y +=
                velocity.Y * dt;

        }





        // =====================================================
        // SHOOTING
        // =====================================================

        private void HandleShooting(
            KeyboardState keyboard,
            KeyboardState previousKeyboard,
            List<Projectile> projectiles)
        {

            if (EvolutionStage < 1)
                return;



            if (keyboard.IsKeyDown(Keys.K)
                &&
               previousKeyboard.IsKeyUp(Keys.K))
            {

                Shoot(projectiles);

            }

        }





        private void Shoot(
            List<Projectile> projectiles)
        {

            Vector2 spawn =
                position +
                new Vector2(
                    size.X / 2,
                    size.Y / 2);



            // Evolution 1

            if (EvolutionStage == 1)
            {

                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            0),
                        true));

            }




            // Evolution 2+

            if (EvolutionStage >= 2)
            {

                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            0),
                        true));



                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            -0.25f),
                        true));



                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            0.25f),
                        true));

            }

        }





        // =====================================================
        // ATTACK
        // =====================================================

        private void HandleAttack(
            KeyboardState keyboard,
            KeyboardState previousKeyboard,
            float dt)
        {


            if (_attackTimer > 0)
            {

                _attackTimer -= dt;

                IsAttacking = true;

            }
            else
            {

                IsAttacking = false;

            }




            if (keyboard.IsKeyDown(Keys.J)
                &&
               previousKeyboard.IsKeyUp(Keys.J)
                &&
               _attackTimer <= 0)
            {

                _attackTimer =
                    _attackCooldown;

            }

        }





        // =====================================================
        // COLLISION
        // =====================================================

        public void ApplyCollision(
            List<Rectangle> solids)
        {

            _grounded = false;



            Rectangle box =
                Bounds;



            foreach (Rectangle tile in solids)
            {

                if (!box.Intersects(tile))
                    continue;



                Rectangle overlap =
                    Rectangle.Intersect(
                        box,
                        tile);



                // Vertical collision

                if (overlap.Height < overlap.Width)
                {

                    if (position.Y < tile.Top)
                    {

                        position.Y =
                            tile.Top - size.Y;



                        velocity.Y = 0;



                        _grounded = true;


                        // reset double jump

                        _doubleJumpUsed = false;

                    }
                    else
                    {

                        position.Y =
                            tile.Bottom;


                        velocity.Y = 0;

                    }

                }



                // Horizontal collision

                else
                {

                    if (position.X < tile.Left)
                    {
                        position.X =
                            tile.Left - size.X;
                    }
                    else
                    {
                        position.X =
                            tile.Right;
                    }

                }



                box =
                    Bounds;

            }

        }
        // =====================================================
        // EVOLUTION SYSTEM
        // =====================================================

        public void Evolve(int stage)
        {
            if (stage <= EvolutionStage)
                return;


            EvolutionStage = stage;


            // keep current max health
            Health = Math.Min(
                Health,
                MaxHealth);


            // Save evolution permanently
            SaveCheckpoint();


            if (stage == 1)
            {
                HasShownShootMessage = false;
            }


            if (stage == 2)
            {
                HasShownDoubleJumpMessage = false;
            }
        }





        // =====================================================
        // CHECKPOINT SYSTEM
        // =====================================================

        public void SaveCheckpoint()
        {
            SpawnPoint = position;


            _checkpointEvolution = EvolutionStage;


            _checkpointMaxHealth = MaxHealth;


            _checkpointHealth = Health;
        }





        public void RespawnAtCheckpoint()
        {
            position = SpawnPoint;

            velocity = Vector2.Zero;


            EvolutionStage =
                _checkpointEvolution;


            MaxHealth =
                _checkpointMaxHealth;


            Health =
                _checkpointMaxHealth;


            _doubleJumpUsed = false;

            _grounded = false;

            _invincibleTimer =
                _invincibleDuration;
        }





        // =====================================================
        // HEALTH UPGRADE
        // =====================================================

        public void IncreaseHealth()
        {
            Health++;


            // Save permanent upgrade
            SaveCheckpoint();
        }





        public void HealFull()
        {

            Health =
                MaxHealth;

        }





        // =====================================================
        // MOVEMENT SPEED
        // =====================================================

        private float GetSpeed()
        {

            switch (EvolutionStage)
            {

                case 1:
                    return 260f;


                case 2:
                    return 300f;


                case 3:
                    return 340f;


                default:
                    return _speed;

            }

        }





        // =====================================================
        // ATTACK HITBOX
        // =====================================================

        public Rectangle AttackHitbox
        {
            get
            {

                if (!IsAttacking)
                    return Rectangle.Empty;



                if (FacingDirection == 1)
                {

                    return new Rectangle(
                        Bounds.Right,
                        Bounds.Y,
                        35,
                        Bounds.Height);

                }
                else
                {

                    return new Rectangle(
                        Bounds.Left - 35,
                        Bounds.Y,
                        35,
                        Bounds.Height);

                }

            }

        }





        // =====================================================
        // DAMAGE FROM ENEMIES
        // =====================================================

        public void CheckEnemyCollision(
            List<Enemy> enemies)
        {

            if (_invincibleTimer > 0)
                return;



            foreach (var enemy in enemies)
            {

                if (enemy.IsDead)
                    continue;



                if (Bounds.Intersects(enemy.Bounds))
                {

                    Health--;



                    _invincibleTimer =
                        _invincibleDuration;



                    velocity.Y =
                        -350f;



                    if (position.X <
                       enemy.position.X)
                    {
                        position.X -= 40;
                    }
                    else
                    {
                        position.X += 40;
                    }



                    break;

                }

            }

        }





        // =====================================================
        // DEATH
        // =====================================================

        public bool IsDead()
        {

            return Health <= 0;

        }





        public void CheckDeath()
        {

            if (Health <= 0)
            {

                RespawnAtCheckpoint();

            }

        }





        // =====================================================
        // RESET
        // =====================================================

        public void ResetVelocity()
        {

            velocity =
                Vector2.Zero;

        }





        // =====================================================
        // DRAW
        // =====================================================

        public override void Draw(
            SpriteBatch spriteBatch)
        {

            Color color =
                Color.White;



            switch (EvolutionStage)
            {

                case 1:
                    color =
                        Color.LightGreen;
                    break;


                case 2:
                    color =
                        Color.Cyan;
                    break;


                case 3:
                    color =
                        Color.Gold;
                    break;

            }



            if (IsAttacking)
            {
                color =
                    Color.Orange;
            }



            if (_invincibleTimer > 0)
            {
                color =
                    Color.LightBlue;
            }



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

        }

    }
}