using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Systems;
using TheLostRobotStory.World;

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



        // =====================================================
        // PLATFORM
        // =====================================================

        private bool _standingOnPlatform;



        // =====================================================
        // COYOTE + BUFFER JUMP
        // =====================================================

        private float _coyoteTimer;

        private float _coyoteTime = 0.15f;


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
        // ANIMATION
        // =====================================================

        private AnimationSystem _animation;



        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Player(Vector2 startPosition)
        {

            position = startPosition;


            SpawnPoint = startPosition;


            size = new Vector2(
                32,
                32);



            _animation =
                new AnimationSystem();



            SaveCheckpoint();

        }






        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime,
            InputManager input,
            List<Projectile> projectiles,
            List<Rectangle> solids,
            List<MovingPlatform> platforms)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            _standingOnPlatform = false;



            HandleMovement(
                input,
                dt);



            HandleJump(
                input,
                dt);



            ApplyGravity(
                dt);



            HandleShooting(
                input,
                projectiles);



            HandleAttack(
                input,
                dt);



            _animation.Update(
                gameTime);



            if (_invincibleTimer > 0)
            {

                _invincibleTimer -= dt;

            }

        }






        // =====================================================
        // MOVEMENT
        // =====================================================

        private void HandleMovement(
            InputManager input,
            float dt)
        {

            float speed =
                GetSpeed();



            velocity.X = 0;



            if (input.Left())
            {

                velocity.X =
                    -speed;


                FacingDirection =
                    -1;

            }



            if (input.Right())
            {

                velocity.X =
                    speed;


                FacingDirection =
                    1;

            }



            position.X +=
                velocity.X * dt;

        }







        // =====================================================
        // SPEED BY EVOLUTION
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
        // JUMP SYSTEM
        // =====================================================

        private void HandleJump(
            InputManager input,
            float dt)
        {


            // Coyote time

            if (_grounded)
            {

                _coyoteTimer =
                    _coyoteTime;


                _doubleJumpUsed =
                    false;

            }
            else
            {

                _coyoteTimer -= dt;

            }





            // Jump buffer

            if (input.JumpPressed())
            {

                _jumpBufferTimer =
                    _jumpBufferTime;

            }
            else
            {

                _jumpBufferTimer -= dt;

            }






            if (_jumpBufferTimer > 0)
            {


                // Normal jump

                if (_coyoteTimer > 0)
                {

                    velocity.Y =
                        _jumpForce;



                    _grounded =
                        false;



                    _jumpBufferTimer =
                        0;



                    _coyoteTimer =
                        0;

                }



                // Evolution 2 double jump

                else if (
                    EvolutionStage >= 2
                    &&
                    !_doubleJumpUsed)
                {

                    velocity.Y =
                        _jumpForce;



                    _doubleJumpUsed =
                        true;



                    _jumpBufferTimer =
                        0;

                }

            }

        }







        // =====================================================
        // GRAVITY
        // =====================================================

        private void ApplyGravity(
            float dt)
        {

            velocity.Y +=
                _gravity * dt;



            if (velocity.Y >
                _maxFallSpeed)
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
            InputManager input,
            List<Projectile> projectiles)
        {

            if (EvolutionStage < 1)
                return;



            if (input.ShootPressed())
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
            InputManager input,
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







            if (input.AttackPressed()
                &&
                _attackTimer <= 0)
            {

                _attackTimer =
                    _attackCooldown;

            }

        }









        // =====================================================
        // SOLID COLLISION
        // =====================================================

        public void ApplyCollision(
            List<Rectangle> solids)
        {

            _grounded = false;



            Rectangle playerBox =
                Bounds;




            foreach (Rectangle tile in solids)
            {


                if (!playerBox.Intersects(tile))
                    continue;





                Rectangle overlap =
                    Rectangle.Intersect(
                        playerBox,
                        tile);






                // Vertical collision

                if (overlap.Height < overlap.Width)
                {


                    // Landing

                    if (position.Y < tile.Top)
                    {

                        position.Y =
                            tile.Top - size.Y;



                        velocity.Y = 0;



                        _grounded = true;



                        _doubleJumpUsed = false;

                    }




                    // Ceiling

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




                playerBox =
                    Bounds;

            }

        }









        // =====================================================
        // MOVING PLATFORM COLLISION
        // =====================================================

        public void ApplyPlatformCollision(
            List<MovingPlatform> platforms)
        {


            foreach (MovingPlatform platform in platforms)
            {


                if (!Bounds.Intersects(
                    platform.Bounds))
                    continue;





                Rectangle overlap =
                    Rectangle.Intersect(
                        Bounds,
                        platform.Bounds);







                // Player landing on platform

                if (overlap.Height < overlap.Width
                    &&
                    velocity.Y >= 0
                    &&
                    position.Y < platform.Bounds.Top)
                {


                    position.Y =
                        platform.Bounds.Top - size.Y;



                    velocity.Y = 0;



                    _grounded = true;


                    _doubleJumpUsed = false;



                    _standingOnPlatform = true;




                    // Move exactly with platform

                    position +=
                        platform.Velocity;



                    break;

                }

            }

        }







        // =====================================================
        // PLATFORM STATE
        // =====================================================

        public void SetGrounded(
            bool value)
        {

            _grounded = value;



            if (value)
            {

                _doubleJumpUsed = false;

            }

        }
        // =====================================================
        // EVOLUTION SYSTEM
        // =====================================================

        public void Evolve(
            int stage)
        {

            if (stage <= EvolutionStage)
                return;



            EvolutionStage =
                stage;



            // IMPORTANT:
            // Evolution NEVER changes health.
            // Extra lives stay.

            SaveCheckpoint();




            if (stage == 1)
            {

                HasShownShootMessage =
                    false;

            }




            if (stage == 2)
            {

                HasShownDoubleJumpMessage =
                    false;

            }

        }







        // =====================================================
        // CHECKPOINT SYSTEM
        // =====================================================

        public void SaveCheckpoint()
        {

            SpawnPoint =
                position;



            // Keep highest evolution

            if (EvolutionStage >
                _checkpointEvolution)
            {

                _checkpointEvolution =
                    EvolutionStage;

            }




            // Keep highest max health

            if (MaxHealth >
                _checkpointMaxHealth)
            {

                _checkpointMaxHealth =
                    MaxHealth;

            }




            // Keep current health if higher

            if (Health >
                _checkpointHealth)
            {

                _checkpointHealth =
                    Health;

            }

        }







        public void RespawnAtCheckpoint()
        {

            position =
                SpawnPoint;



            velocity =
                Vector2.Zero;




            EvolutionStage =
                _checkpointEvolution;



            MaxHealth =
                _checkpointMaxHealth;



            // Respawn with full upgraded health

            Health =
                MaxHealth;




            _grounded =
                false;



            _doubleJumpUsed =
                false;



            _invincibleTimer =
                _invincibleDuration;

        }







        // =====================================================
        // HEALTH SYSTEM
        // =====================================================

        public void IncreaseHealth()
        {

            // +1 maximum life

            MaxHealth++;



            // Fill new health

            Health =
                MaxHealth;



            SaveCheckpoint();

        }






        public void HealFull()
        {

            Health =
                MaxHealth;

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
        // DAMAGE SYSTEM
        // =====================================================

        public void TakeDamage(
            int damage)
        {

            if (_invincibleTimer > 0)
                return;



            Health -=
                damage;



            if (Health < 0)
            {

                Health = 0;

            }




            _invincibleTimer =
                _invincibleDuration;



            velocity.Y =
                -350f;

        }









        public void CheckEnemyCollision(
            List<Enemy> enemies)
        {

            if (_invincibleTimer > 0)
                return;




            foreach (Enemy enemy in enemies)
            {

                if (enemy.IsDead)
                    continue;




                if (Bounds.Intersects(
                    enemy.Bounds))
                {

                    TakeDamage(1);




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

            Texture2D texture =
                TextureManager.Player
                ??
                TextureManager.Pixel;



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
                texture,
                Bounds,
                color);

        }

    }
}
