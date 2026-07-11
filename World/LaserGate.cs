using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public class LaserGate
    {

        // =====================================================
        // POSITION
        // =====================================================

        public Vector2 Position;


        public Rectangle Bounds;



        // =====================================================
        // STATE
        // =====================================================

        public bool IsActive;



        private float _timer;



        // laser timing

        private float _onTime = 2f;

        private float _offTime = 2f;



        // =====================================================
        // DAMAGE
        // =====================================================

        private int _damage = 1;


        private float _damageCooldown = 1f;

        private float _damageTimer;



        // =====================================================
        // PARTICLES
        // =====================================================

        private ParticleManager _particles;


        private float _particleTimer;



        // =====================================================
        // ANIMATION
        // =====================================================

        private float _pulse;



        private const int Width = 16;

        private const int Height = 96;



        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public LaserGate(
            Vector2 position,
            ParticleManager particles)
        {

            Position = position;

            _particles = particles;


            Bounds =
                new Rectangle(
                    (int)position.X,
                    (int)position.Y,
                    Width,
                    Height);


            // start disabled

            IsActive = false;

        }







        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime,
            Player player)
        {

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;



            _timer += dt;



            _pulse += dt;



            if (_damageTimer > 0)
                _damageTimer -= dt;



            // =========================================
            // LASER ON/OFF TIMER
            // =========================================

            if (IsActive)
            {

                if (_timer >= _onTime)
                {
                    IsActive = false;

                    _timer = 0;
                }

            }

            else
            {

                if (_timer >= _offTime)
                {
                    IsActive = true;

                    _timer = 0;
                }

            }






            // =========================================
            // PARTICLES ONLY WHEN ACTIVE
            // =========================================

            if (IsActive)
            {

                _particleTimer += dt;


                if (_particleTimer >= 0.15f)
                {

                    _particleTimer = 0;


                    Vector2 spark =
                        new Vector2(
                            Bounds.Center.X,
                            Bounds.Y +
                            Random.Shared.Next(
                                Bounds.Height));


                    _particles.SpawnExplosion(
                        spark,
                        Color.Red);

                }

            }







            // =========================================
            // DAMAGE
            // =========================================

            if (IsActive)
            {

                if (player.Bounds.Intersects(Bounds))
                {

                    if (_damageTimer <= 0)
                    {

                        player.TakeDamage(
                            _damage);


                        _damageTimer =
                            _damageCooldown;


                        _particles.SpawnExplosion(
                            player.position,
                            Color.Red);

                    }

                }

            }

        }








        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {


            Color laserColor;



            if (IsActive)
            {

                float glow =
                    0.7f +
                    (float)System.Math.Sin(
                        _pulse * 8)
                    * 0.3f;


                laserColor =
                    Color.Red * glow;

            }

            else
            {

                // inactive laser

                laserColor =
                    Color.DarkRed * 0.25f;

            }




            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                laserColor);





            // bright centre only when active

            if (IsActive)
            {

                Rectangle core =
                    new Rectangle(
                        Bounds.X + 5,
                        Bounds.Y,
                        6,
                        Bounds.Height);



                spriteBatch.Draw(
                    TextureManager.Pixel,
                    core,
                    Color.White);

            }

        }

    }
}