using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    public enum CollectibleType
    {
        EnergyCrystal,
        HealthUpgrade
    }



    public class Collectible
    {

        // =====================================================
        // TYPE
        // =====================================================

        public CollectibleType Type;



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



        // =====================================================
        // ANIMATION
        // =====================================================

        private float _floatTimer;

        private float _startY;



        private const int Size = 16;




        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Collectible(
            Vector2 position,
            CollectibleType type = CollectibleType.EnergyCrystal)
        {

            Position = position;

            _startY = position.Y;


            Type = type;



            Bounds =
                new Rectangle(
                    (int)position.X,
                    (int)position.Y,
                    Size,
                    Size);

        }





        // =====================================================
        // COLLECT
        // =====================================================

        public void Collect(Player player)
        {

            if (IsCollected)
                return;



            IsCollected = true;



            switch (Type)
            {

                // 🟡 Energy crystal
                // Used for door completion
                case CollectibleType.EnergyCrystal:

                    break;



                // ❤️ Health upgrade
                case CollectibleType.HealthUpgrade:

                    player.IncreaseHealth();

                    break;

            }

        }





        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(GameTime gameTime)
        {

            if (IsCollected)
                return;



            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;



            _floatTimer += dt * 3f;



            float offset =
                (float)System.Math.Sin(
                    _floatTimer)
                    * 3f;



            Position.Y =
                _startY + offset;



            Bounds =
                new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    Size,
                    Size);

        }





        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            if (IsCollected)
                return;



            Color color =
                Color.Gold;



            switch (Type)
            {

                // 🟡 Energy crystal
                case CollectibleType.EnergyCrystal:

                    color =
                        Color.Gold;

                    break;



                // ❤️ Health upgrade
                case CollectibleType.HealthUpgrade:

                    color =
                        Color.Red;

                    break;

            }



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

        }

    }
}