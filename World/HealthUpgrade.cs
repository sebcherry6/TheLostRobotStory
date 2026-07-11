using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public class HealthUpgrade
    {

        public Vector2 Position;

        public Rectangle Bounds;

        public bool IsCollected;


        private float _floatTimer;

        private const int Size = 24;



        public HealthUpgrade(Vector2 position)
        {

            Position = position;


            UpdateBounds();

        }





        private void UpdateBounds()
        {

            Bounds =
                new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    Size,
                    Size);

        }





        public void Update(
            GameTime gameTime)
        {

            if (IsCollected)
                return;



            _floatTimer +=
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            Position.Y +=
                (float)System.Math.Sin(
                    _floatTimer * 3f)
                    * 0.2f;



            UpdateBounds();

        }






        public void Collect(
            Player player)
        {

            if (IsCollected)
                return;



            IsCollected = true;



            player.IncreaseHealth();

        }






        public void Draw(
                    SpriteBatch spriteBatch)
        {

            if (IsCollected)
                return;



            // Glow
            Rectangle glow =
                new Rectangle(
                    Bounds.X - 4,
                    Bounds.Y - 4,
                    Size + 8,
                    Size + 8);



            spriteBatch.Draw(
                TextureManager.Pixel,
                glow,
                Color.Red * 0.4f);




            // Main upgrade
            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                Color.Red);




            // White centre
            Rectangle centre =
                new Rectangle(
                    Bounds.X + 6,
                    Bounds.Y + 6,
                    12,
                    12);



            spriteBatch.Draw(
                TextureManager.Pixel,
                centre,
                Color.White);

        }

    }
}