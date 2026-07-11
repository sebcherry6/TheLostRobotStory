using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{

    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Laser
    }



    public class Enemy : Entity
    {

        // =====================================================
        // TYPE
        // =====================================================

        public EnemyType Type;



        // =====================================================
        // HEALTH
        // =====================================================

        public int Health = 1;


        public bool IsDead =>
            Health <= 0;



        private float _hitCooldown;

        private float _flashTimer;





        // =====================================================
        // MOVEMENT
        // =====================================================

        protected float _speed;

        protected int _direction = 1;


        private float _gravity = 1800f;

        private float _maxFallSpeed = 900f;






        // =====================================================
        // SHOOTING
        // =====================================================

        protected float _shootTimer = 2f;

        protected float _shootCooldown = 2f;






        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Enemy(
            Vector2 startPos,
            EnemyType type)
        {

            position =
                startPos;


            size =
                new Vector2(
                    32,
                    32);



            Type =
                type;



            switch (Type)
            {

                case EnemyType.Normal:

                    _speed = 80f;
                    Health = 2;

                    break;



                case EnemyType.Fast:

                    _speed = 140f;
                    Health = 1;

                    break;



                case EnemyType.Tank:

                    _speed = 50f;
                    Health = 5;

                    break;



                case EnemyType.Laser:

                    _speed = 70f;
                    Health = 3;

                    break;

            }

        }








        // =====================================================
        // DAMAGE
        // =====================================================

        public virtual void TakeDamage(
            int damage)
        {

            if (_hitCooldown > 0)
                return;



            Health -= damage;


            _hitCooldown =
                0.15f;



            _flashTimer =
                0.15f;

        }









        // =====================================================
        // UPDATE
        // =====================================================

        public virtual void Update(
            GameTime gameTime,
            List<Rectangle> solids,
            Player player,
            List<Projectile> projectiles)
        {

            if (IsDead)
                return;



            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            if (_hitCooldown > 0)
                _hitCooldown -= dt;



            if (_flashTimer > 0)
                _flashTimer -= dt;




            Move(
                solids,
                dt);



            if (Type == EnemyType.Laser)
            {

                Shoot(
                    player,
                    projectiles,
                    dt);

            }


        }








        // =====================================================
        // MOVEMENT
        // =====================================================

        protected virtual void Move(
            List<Rectangle> solids,
            float dt)
        {


            // horizontal

            position.X +=
                _direction *
                _speed *
                dt;



            foreach (Rectangle tile in solids)
            {

                if (!Bounds.Intersects(tile))
                    continue;



                if (_direction > 0)
                {

                    position.X =
                        tile.Left - size.X;

                }
                else
                {

                    position.X =
                        tile.Right;

                }



                _direction *= -1;

                break;

            }





            // gravity

            velocity.Y +=
                _gravity *
                dt;



            if (velocity.Y >
                _maxFallSpeed)
            {
                velocity.Y =
                    _maxFallSpeed;
            }



            position.Y +=
                velocity.Y *
                dt;




            foreach (Rectangle tile in solids)
            {

                if (!Bounds.Intersects(tile))
                    continue;



                if (velocity.Y > 0)
                {

                    position.Y =
                        tile.Top -
                        size.Y;

                }
                else
                {

                    position.Y =
                        tile.Bottom;

                }


                velocity.Y = 0;

            }


        }









        // =====================================================
        // SHOOT
        // =====================================================

        protected virtual void Shoot(
            Player player,
            List<Projectile> projectiles,
            float dt)
        {

            _shootTimer -= dt;



            if (_shootTimer > 0)
                return;



            _shootTimer =
                _shootCooldown;




            Vector2 direction =
                player.position -
                position;



            if (direction != Vector2.Zero)
                direction.Normalize();




            projectiles.Add(
                new Projectile(
                    position +
                    new Vector2(
                        size.X / 2,
                        size.Y / 2),
                    direction,
                    false));

        }









        // =====================================================
        // DRAW
        // =====================================================

        public override void Draw(
            SpriteBatch spriteBatch)
        {

            if (IsDead)
                return;



            Color color =
                Color.Red;



            switch (Type)
            {

                case EnemyType.Fast:

                    color =
                        Color.Orange;

                    break;



                case EnemyType.Tank:

                    color =
                        Color.DarkRed;

                    break;



                case EnemyType.Laser:

                    color =
                        Color.Purple;

                    break;

            }



            if (_flashTimer > 0)
            {
                color =
                    Color.White;
            }



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

        }

    }

}