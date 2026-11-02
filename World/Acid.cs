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

        // set by Level from levels.json (green acid, molten metal...)
        public static Color Tint = Color.LimeGreen;

        // acid.png: frames of 32x64 -> top half = surface, bottom half = deep liquid.
        // Level picks "acid_<level>.png" when it exists (e.g. molten metal).
        public static Texture2D Sheet;

        // false when another acid tile sits on top (draws the deep part)
        public bool IsSurface = true;





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
                    Tint);

            }








            // =================================================
            // PLAYER DAMAGE
            // =================================================

            if (PixelCollision.Overlaps(player.GetShape(), GetShape()))
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
                        Tint);



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

        private Rectangle SourceRect(Texture2D sheet)
        {
            int frame = Sprites.AnimFrame(sheet, Size, 5f);
            bool twoRows = sheet.Height >= Size * 2;
            int row = twoRows && !IsSurface ? 1 : 0;

            return new Rectangle(frame * Size, row * Size, Size, twoRows ? Size : sheet.Height);
        }

        public SpriteShape GetShape()
        {
            Texture2D sheet = Sheet ?? TextureManager.Acid;

            return sheet == null
                ? SpriteShape.FromRect(Bounds)
                : SpriteShape.FromTexture(sheet, SourceRect(sheet), Bounds);
        }

        public void Draw(
            SpriteBatch spriteBatch)
        {
            Texture2D sheet = Sheet ?? TextureManager.Acid;

            if (sheet != null)
            {
                // every tile shares one clock so a pool moves as one surface
                spriteBatch.Draw(sheet, Bounds, SourceRect(sheet), Color.White);
                return;
            }


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
                Tint);

        }

    }
}