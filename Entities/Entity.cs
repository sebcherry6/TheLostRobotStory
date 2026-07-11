using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheLostRobotStory.Entities
{
    public abstract class Entity
    {

        // =====================================================
        // TRANSFORM
        // =====================================================

        public Vector2 position;

        public Vector2 velocity;

        public Vector2 size;



        // =====================================================
        // STATE
        // =====================================================

        public bool IsOnGround;



        // =====================================================
        // COLLIDER
        // =====================================================

        public virtual Rectangle Bounds
        {
            get
            {
                return new Rectangle(
                    (int)position.X,
                    (int)position.Y,
                    (int)size.X,
                    (int)size.Y);
            }
        }




        // =====================================================
        // UPDATE
        // =====================================================

        public virtual void Update(
            GameTime gameTime)
        {

        }





        // =====================================================
        // DRAW
        // =====================================================

        public virtual void Draw(
            SpriteBatch spriteBatch)
        {

        }





        // =====================================================
        // MOVEMENT HELPERS
        // =====================================================

        protected void MoveX(
            float amount)
        {

            position.X += amount;

        }



        protected void MoveY(
            float amount)
        {

            position.Y += amount;

        }





        // =====================================================
        // GRAVITY
        // =====================================================

        protected void ApplyGravity(
            float gravity)
        {

            velocity.Y += gravity;

        }





        // =====================================================
        // COLLISION RESOLUTION
        // =====================================================

        protected void ResolveCollision(
            List<Rectangle> solids)
        {

            IsOnGround = false;



            Rectangle bounds =
                Bounds;



            foreach (Rectangle tile in solids)
            {

                if (!bounds.Intersects(tile))
                    continue;



                Rectangle overlap =
                    Rectangle.Intersect(
                        bounds,
                        tile);




                // -------------------------
                // Horizontal collision
                // -------------------------

                if (overlap.Width < overlap.Height)
                {

                    if (bounds.Center.X < tile.Center.X)
                    {

                        position.X -= overlap.Width;

                    }
                    else
                    {

                        position.X += overlap.Width;

                    }


                    velocity.X = 0;

                }



                // -------------------------
                // Vertical collision
                // -------------------------

                else
                {

                    if (bounds.Center.Y < tile.Center.Y)
                    {

                        // landing

                        position.Y -= overlap.Height;


                        velocity.Y = 0;


                        IsOnGround = true;

                    }
                    else
                    {

                        // hitting ceiling

                        position.Y += overlap.Height;


                        velocity.Y = 0;

                    }

                }



                bounds =
                    Bounds;

            }

        }

    }
}