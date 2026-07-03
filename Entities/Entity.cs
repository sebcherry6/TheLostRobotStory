using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostRobotStory.Entities
{
    public class Entity
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 size;

        public Rectangle Bounds =>
            new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)size.X,
                (int)size.Y
            );

        // IMPORTANT: match signature used by Enemy/Player
        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }
    }
}