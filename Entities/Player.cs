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
        private float _speed = 220f;
        private float _gravity = 900f;
        private float _jumpForce = -650f;

        private bool _grounded;

        private bool _doubleJumpUsed;



        // =========================
        // ATTACK
        // =========================
        public bool IsAttacking;

        private float _attackTimer;

        private float _attackCooldown = 0.35f;



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



            float moveSpeed = GetSpeed();



            // =========================
            // MOVE
            // =========================

            if (keyboard.IsKeyDown(Keys.A))
            {
                velocity.X = -moveSpeed;
                FacingDirection = -1;
            }
            else if (keyboard.IsKeyDown(Keys.D))
            {
                velocity.X = moveSpeed;
                FacingDirection = 1;
            }
            else
            {
                velocity.X = 0;
            }



            // =========================
            // JUMP
            // =========================

            if (keyboard.IsKeyDown(Keys.Space)
                &&
                previousKeyboard.IsKeyUp(Keys.Space))
            {

                if (_grounded)
                {
                    velocity.Y = _jumpForce;
                    _grounded = false;
                }

                else if (EvolutionStage >= 2 &&
                        !_doubleJumpUsed)
                {
                    velocity.Y = _jumpForce;
                    _doubleJumpUsed = true;
                }
            }



            // =========================
            // GRAVITY
            // =========================

            velocity.Y += _gravity * dt;


            position.X += velocity.X * dt;

            position.Y += velocity.Y * dt;



            // =========================
            // SHOOTING
            // =========================

            if (EvolutionStage >= 1 &&
               keyboard.IsKeyDown(Keys.K) &&
               previousKeyboard.IsKeyUp(Keys.K))
            {

                Shoot(projectiles);

            }



            // =========================
            // MELEE ATTACK
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


            if (keyboard.IsKeyDown(Keys.J)
               &&
               _attackTimer <= 0)
            {
                _attackTimer = _attackCooldown;
            }



            // =========================
            // DAMAGE TIMER
            // =========================

            if (_invincibleTimer > 0)
                _invincibleTimer -= dt;
        }



        // =========================================================
        // SHOOTING
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


            else if (EvolutionStage >= 2)
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
                        -0.3f),
                    true));


                projectiles.Add(
                    new Projectile(
                    spawn,
                    new Vector2(
                        FacingDirection,
                        0.3f),
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
                    Rectangle.Intersect(box, tile);



                // vertical
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


                // horizontal
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
            {
                EvolutionStage = stage;
            }

        }




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
                    return 220f;
            }

        }





        // =========================================================
        // ATTACK HITBOX
        // =========================================================

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

                    Bounds.Height
                );
            }
        }




        // =========================================================
        // DAMAGE
        // =========================================================

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


                    velocity.Y = -200;


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

            Color color =
                Color.White;



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