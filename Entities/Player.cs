using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Player : Entity
    {
        // =========================
        // EVOLUTION
        // =========================
        public int EvolutionStage = 0;


        // =========================
        // HEALTH
        // =========================
        public int Health = 3;

        public Vector2 SpawnPoint;

        public int FacingDirection = 1;



        // =========================
        // MOVEMENT
        // =========================

        private float _gravity = 1800f;
        private float _jumpForce = -1000f;

        private bool _grounded;

        private bool _doubleJumpUsed;


        // coyote time
        private float _coyoteTimer;
        private float _coyoteTime = 0.12f;


        // jump buffer
        private float _jumpBufferTimer;
        private float _jumpBufferTime = 0.15f;


        private float _maxFallSpeed = 900f;



        // =========================
        // ATTACK
        // =========================

        public bool IsAttacking;

        private float _attackTimer;

        private float _attackCooldown = 0.25f;



        // =========================
        // DAMAGE
        // =========================

        private float _invincibleTimer;

        private float _invincibleDuration = 1f;



        public Player(Vector2 startPos)
        {
            position = startPos;

            SpawnPoint = startPos;

            size = new Vector2(32, 32);
        }




        // =========================================================
        // UPDATE
        // =========================================================

        public void Update(
            GameTime gameTime,
            KeyboardState keyboard,
            KeyboardState previousKeyboard,
            List<Projectile> projectiles)
        {

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;



            // =========================
            // MOVEMENT
            // =========================

            float speed = GetSpeed();



            if (keyboard.IsKeyDown(Keys.A))
            {
                velocity.X = -speed;
                FacingDirection = -1;
            }

            else if (keyboard.IsKeyDown(Keys.D))
            {
                velocity.X = speed;
                FacingDirection = 1;
            }

            else
            {
                velocity.X = 0;
            }




            // =========================
            // COYOTE TIMER
            // =========================

            if (_grounded)
                _coyoteTimer = _coyoteTime;

            else
                _coyoteTimer -= dt;




            // =========================
            // JUMP BUFFER
            // =========================

            if (keyboard.IsKeyDown(Keys.Space) &&
               previousKeyboard.IsKeyUp(Keys.Space))
            {
                _jumpBufferTimer = _jumpBufferTime;
            }

            else
            {
                _jumpBufferTimer -= dt;
            }



            TryJump();




            // =========================
            // GRAVITY
            // =========================

            velocity.Y += _gravity * dt;


            if (velocity.Y > _maxFallSpeed)
                velocity.Y = _maxFallSpeed;



            position += velocity * dt;





            // =========================
            // SHOOT
            // =========================

            if (EvolutionStage >= 1 &&
               keyboard.IsKeyDown(Keys.K) &&
               previousKeyboard.IsKeyUp(Keys.K))
            {
                Shoot(projectiles);
            }




            // =========================
            // ATTACK
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



            if (keyboard.IsKeyDown(Keys.J) &&
               _attackTimer <= 0)
            {
                _attackTimer = _attackCooldown;
            }



            // =========================
            // VARIABLE JUMP
            // =========================

            if (keyboard.IsKeyUp(Keys.Space) &&
               velocity.Y < 0)
            {
                velocity.Y += 900f * dt;
            }




            if (_invincibleTimer > 0)
                _invincibleTimer -= dt;

        }




        private void TryJump()
        {

            if (_jumpBufferTimer <= 0)
                return;



            if (_coyoteTimer > 0)
            {

                velocity.Y = _jumpForce;

                _grounded = false;

                _coyoteTimer = 0;

                _jumpBufferTimer = 0;

            }


            else if (EvolutionStage >= 2 &&
                    !_doubleJumpUsed)
            {

                velocity.Y = _jumpForce;

                _doubleJumpUsed = true;

                _jumpBufferTimer = 0;

            }

        }





        // =========================================================
        // SHOOT
        // =========================================================

        private void Shoot(List<Projectile> projectiles)
        {

            Vector2 spawn =
                position +
                new Vector2(size.X / 2, size.Y / 2);



            if (EvolutionStage == 1)
            {

                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(FacingDirection, 0),
                        true));

            }



            if (EvolutionStage >= 2)
            {

                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(FacingDirection, 0),
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




        // =========================================================
        // COLLISION
        // =========================================================

        public void ApplyCollision(List<Rectangle> solids)
        {

            _grounded = false;


            Rectangle box = Bounds;



            foreach (Rectangle tile in solids)
            {

                if (!box.Intersects(tile))
                    continue;



                Rectangle overlap =
                    Rectangle.Intersect(
                        box,
                        tile);



                if (overlap.Height < overlap.Width)
                {

                    if (position.Y < tile.Y)
                    {

                        position.Y =
                            tile.Top - size.Y;


                        velocity.Y = 0;


                        _grounded = true;

                        _doubleJumpUsed = false;

                    }

                    else
                    {

                        position.Y =
                            tile.Bottom;

                        velocity.Y = 0;

                    }

                }

                else
                {

                    if (position.X < tile.X)
                        position.X =
                            tile.Left - size.X;

                    else
                        position.X =
                            tile.Right;

                }



                box = Bounds;

            }

        }




        // =========================================================
        // EVOLUTION
        // =========================================================

        public void Evolve(int stage)
        {
            if (stage > EvolutionStage)
                EvolutionStage = stage;
        }





        private float GetSpeed()
        {

            return EvolutionStage switch
            {
                1 => 260f,
                2 => 300f,
                3 => 340f,
                _ => 220f
            };

        }





        public Rectangle AttackHitbox
        {
            get
            {

                if (!IsAttacking)
                    return Rectangle.Empty;


                return new Rectangle(
                    FacingDirection == 1
                    ? Bounds.Right
                    : Bounds.Left - 30,

                    Bounds.Y,

                    30,

                    Bounds.Height);

            }
        }





        public void CheckEnemyCollision(List<Enemy> enemies)
        {

            if (_invincibleTimer > 0)
                return;


            foreach (var enemy in enemies)
            {

                if (Bounds.Intersects(enemy.Bounds))
                {

                    Health--;

                    _invincibleTimer =
                        _invincibleDuration;


                    velocity.Y = -300;

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

                EvolutionStage = 0;

            }

        }





        public override void Draw(SpriteBatch spriteBatch)
        {

            Color color = Color.White;


            if (EvolutionStage == 1)
                color = Color.LightGreen;


            if (EvolutionStage == 2)
                color = Color.Cyan;


            if (EvolutionStage >= 3)
                color = Color.Gold;



            if (IsAttacking)
                color = Color.Orange;


            if (_invincibleTimer > 0)
                color = Color.LightBlue;



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

        }
    }
}