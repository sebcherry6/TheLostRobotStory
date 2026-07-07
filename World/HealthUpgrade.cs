using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public class HealthUpgrade
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
        // STATE
        // =====================================================

        public bool IsCollected;



        private const int Size = 20;




        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public HealthUpgrade(
            Vector2 position)
        {

            Position = position;



            Bounds = new Rectangle(
                (int)position.X,
                (int)position.Y,
                Size,
                Size);

        }






        // =====================================================
        // COLLECT
        // =====================================================

        public void Collect(
            Player player)
        {

            if (IsCollected)
                return;



            player.IncreaseHealth();



            IsCollected = true;

        }






        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime)
        {

            // Future:
            // floating animation
            // glow effect
            // particles

        }






        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            if (IsCollected)
                return;



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                Color.Red);

        }

    }
}