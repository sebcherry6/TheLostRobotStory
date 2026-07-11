using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public class Acid
    {

        // =====================================================
        // POSITION
        // =====================================================

        public Vector2 Position;



        // =====================================================
        // COLLISION
        // =====================================================

        public Rectangle Bounds;



        // =====================================================
        // DAMAGE SYSTEM
        // =====================================================

        private int _damage = 1;


        private float _damageCooldown = 1f;

        private float _damageTimer;




        // =====================================================
        // PARTICLES
        // =====================================================

        private ParticleManager _particles;


        private float _particleTimer;


        private float _particleInterval = 0.25f;




        // =====================================================
        // ANIMATION
        // =====================================================

        private float _animationTimer;

        private float _waveOffset;




        // =====================================================
        // SIZE
        // =====================================================

        private const int Size = 32;





        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Acid(
            Vector2 position,
            ParticleManager particles)
        {

            Position = position;


            _particles = particles;



            Bounds =
                new Rectangle(
                    (int)position.X,
                    (int)position.Y,
                    Size,
                    Size);

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



            _animationTimer += dt;



            _particleTimer += dt;



            if (_damageTimer > 0)
            {
                _damageTimer -= dt;
            }






            // =================================================
            // ACID BUBBLES
            // =================================================

            if (_particleTimer >= _particleInterval)
            {

                _particleTimer = 0;



                Vector2 bubblePosition =
                    new Vector2(
                        Bounds.Center.X,
                        Bounds.Top);



                _particles.SpawnBubble(
                    bubblePosition,
                    Color.LimeGreen);

            }








            // =================================================
            // PLAYER DAMAGE
            // =================================================

            if (player.Bounds.Intersects(Bounds))
            {

                if (_damageTimer <= 0)
                {

                    player.TakeDamage(
                        _damage);



                    _damageTimer =
                        _damageCooldown;



                    // splash effect

                    _particles.SpawnExplosion(
                        player.position,
                        Color.GreenYellow);



                    // knockback

                    player.velocity.Y =
                        -350f;


                }

            }







            // =================================================
            // WAVE ANIMATION
            // =================================================

            _waveOffset =
                (float)System.Math.Sin(
                    _animationTimer * 5f);

        }








        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            Rectangle drawArea =
                new Rectangle(
                    Bounds.X,
                    Bounds.Y +
                    (int)(_waveOffset * 2),
                    Bounds.Width,
                    Bounds.Height);



            spriteBatch.Draw(
                TextureManager.Pixel,
                drawArea,
                Color.LimeGreen);

        }

    }
}