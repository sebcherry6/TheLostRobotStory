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
        public EnemyType Type;


        public int Health = 1;

        public bool IsDead => Health <= 0;


        protected float _speed;

        protected int _direction = 1;


        protected float _shootTimer = 2f;


        protected float _hitCooldown;



        public Enemy(Vector2 startPos, EnemyType type)
        {
            position = startPos;

            size = new Vector2(32, 32);


            Type = type;



            switch (Type)
            {
                case EnemyType.Normal:

                    _speed = 2f;
                    Health = 2;

                    break;


                case EnemyType.Fast:

                    _speed = 4f;
                    Health = 1;

                    break;



                case EnemyType.Tank:

                    _speed = 1.5f;
                    Health = 5;

                    break;



                case EnemyType.Laser:

                    _speed = 2.5f;
                    Health = 3;

                    break;
            }
        }



        // =========================
        // DAMAGE
        // =========================

        public virtual void TakeDamage(int damage)
        {

            if (_hitCooldown > 0)
                return;


            Health -= damage;


            _hitCooldown = 0.15f;

        }





        // =========================
        // UPDATE
        // =========================

        public virtual void Update(
            List<Rectangle> solids,
            Player player,
            List<Projectile> projectiles)
        {

            if (IsDead)
                return;



            if (_hitCooldown > 0)
                _hitCooldown -= 1f / 60f;




            Move(solids);



            if (Type == EnemyType.Laser)
            {
                Shoot(player, projectiles);
            }

        }




        // =========================
        // MOVEMENT
        // =========================

        protected virtual void Move(
            List<Rectangle> solids)
        {


            position.X +=
                _speed * _direction;



            Rectangle box = Bounds;



            foreach (Rectangle tile in solids)
            {

                if (box.Intersects(tile))
                {

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

            }





            // gravity

            velocity.Y += 0.5f;


            position.Y += velocity.Y;



            box = Bounds;



            foreach (Rectangle tile in solids)
            {

                if (box.Intersects(tile))
                {


                    if (velocity.Y > 0)
                    {
                        position.Y =
                            tile.Top - size.Y;
                    }
                    else
                    {
                        position.Y =
                            tile.Bottom;
                    }


                    velocity.Y = 0;

                    break;

                }

            }

        }





        // =========================
        // LASER ENEMY SHOOT
        // =========================

        protected virtual void Shoot(
            Player player,
            List<Projectile> projectiles)
        {

            _shootTimer -= 1f / 60f;


            if (_shootTimer <= 0)
            {

                _shootTimer = 2f;



                Vector2 direction =
                    player.position - position;



                if (direction != Vector2.Zero)
                    direction.Normalize();



                projectiles.Add(
                    new Projectile(
                        position +
                        new Vector2(size.X / 2, size.Y / 2),
                        direction,
                        false)
                );

            }

        }





        // =========================
        // DRAW
        // =========================

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

                    color = Color.Orange;

                    break;



                case EnemyType.Tank:

                    color = Color.DarkRed;

                    break;



                case EnemyType.Laser:

                    color = Color.Purple;

                    break;

            }



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

        }

    }
}