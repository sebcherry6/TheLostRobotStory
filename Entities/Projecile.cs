using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Projectile
    {

        // =====================================================
        // POSITION
        // =====================================================

        public Vector2 Position;

        public Vector2 Velocity;




        // =====================================================
        // STATE
        // =====================================================

        public bool IsDead;


        // true = player bullet
        // false = enemy bullet
        public bool FromPlayer;



        // =====================================================
        // SETTINGS
        // =====================================================

        private const float Speed = 300f;

        private const int Size = 8;

        private const int Damage = 1;





        // =====================================================
        // COLLISION
        // =====================================================

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    Size,
                    Size);
            }
        }







        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Projectile(
            Vector2 startPosition,
            Vector2 direction,
            bool fromPlayer = true)
        {

            Position =
                startPosition;



            FromPlayer =
                fromPlayer;



            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }



            Velocity =
                direction * Speed;

        }








        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime,
            List<Rectangle> solids)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            Position +=
                Velocity * dt;





            foreach (Rectangle tile in solids)
            {

                if (Bounds.Intersects(tile))
                {
                    IsDead = true;

                    return;
                }

            }

        }








        // =====================================================
        // HIT ENEMY
        // =====================================================

        public bool HitEnemy(
            Enemy enemy)
        {

            if (!FromPlayer)
                return false;



            if (enemy.IsDead)
                return false;




            if (!Bounds.Intersects(
                enemy.Bounds))
                return false;




            enemy.TakeDamage(
                Damage);



            IsDead = true;


            return true;

        }








        // =====================================================
        // HIT PLAYER
        // =====================================================

        public bool HitPlayer(
            Player player)
        {

            if (FromPlayer)
                return false;



            if (!Bounds.Intersects(
                player.Bounds))
                return false;




            player.TakeDamage(
                Damage);



            IsDead = true;



            return true;

        }








        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                FromPlayer
                ? Color.Yellow
                : Color.Red);

        }

    }
}