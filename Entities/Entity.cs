using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheLostRobotStory.Entities
{
    public abstract class Entity
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 size;

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

        public bool IsOnGround;

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }

        protected void ApplyGravity(float gravity = 0.5f)
        {
            velocity.Y += gravity;
        }

        protected void MoveHorizontal(float amount)
        {
            position.X += amount;
        }

        protected void MoveVertical()
        {
            position.Y += velocity.Y;
        }

        protected void ResolveCollisions(List<Rectangle> solids)
        {
            Rectangle box = Bounds;

            foreach (Rectangle tile in solids)
            {
                if (!box.Intersects(tile))
                    continue;

                Rectangle overlap = Rectangle.Intersect(box, tile);

                if (overlap.Width < overlap.Height)
                {
                    if (box.Center.X < tile.Center.X)
                        position.X -= overlap.Width;
                    else
                        position.X += overlap.Width;
                }
                else
                {
                    if (box.Center.Y < tile.Center.Y)
                    {
                        position.Y -= overlap.Height;
                        velocity.Y = 0;
                        IsOnGround = true;
                    }
                    else
                    {
                        position.Y += overlap.Height;
                        velocity.Y = 0;
                    }
                }

                box = Bounds;
            }
        }
    }
}