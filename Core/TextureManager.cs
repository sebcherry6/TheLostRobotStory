using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostRobotStory.Core
{
    public static class TextureManager
    {

        // =====================================================
        // BASIC TEXTURES
        // =====================================================

        public static Texture2D Pixel { get; private set; }



        // =====================================================
        // ENTITIES
        // =====================================================

        public static Texture2D Player { get; private set; }

        public static Texture2D Enemy { get; private set; }

        public static Texture2D Boss { get; private set; }

        public static Texture2D Projectile { get; private set; }



        // =====================================================
        // WORLD OBJECTS
        // =====================================================

        public static Texture2D Tile { get; private set; }

        public static Texture2D Door { get; private set; }

        public static Texture2D Crystal { get; private set; }

        public static Texture2D EvolutionCore { get; private set; }

        public static Texture2D HealthUpgrade { get; private set; }



        // =====================================================
        // OBSTACLES
        // =====================================================

        public static Texture2D Acid { get; private set; }

        public static Texture2D Laser { get; private set; }

        public static Texture2D MovingPlatform { get; private set; }

        public static Texture2D Checkpoint { get; private set; }





        // =====================================================
        // LOAD ALL TEXTURES
        // =====================================================

        public static void Load(
            ContentManager content,
            GraphicsDevice graphicsDevice)
        {


            // =================================================
            // CREATE PIXEL TEXTURE
            // =================================================

            Pixel =
                new Texture2D(
                    graphicsDevice,
                    1,
                    1);



            Pixel.SetData(
                new[]
                {
                    Microsoft.Xna.Framework.Color.White
                });





            // =================================================
            // LOAD GAME TEXTURES
            // =================================================
            //
            // Uncomment these when you add images
            //
            // Example:
            //
            // Player =
            //     content.Load<Texture2D>("Textures/player");
            //
            //
            // File:
            // Content/Textures/player.png
            //



            /*
            
            Player =
                content.Load<Texture2D>(
                    "Textures/player");


            Enemy =
                content.Load<Texture2D>(
                    "Textures/enemy");


            Boss =
                content.Load<Texture2D>(
                    "Textures/boss");


            Projectile =
                content.Load<Texture2D>(
                    "Textures/projectile");



            Tile =
                content.Load<Texture2D>(
                    "Textures/tile");


            Door =
                content.Load<Texture2D>(
                    "Textures/door");


            Crystal =
                content.Load<Texture2D>(
                    "Textures/crystal");


            EvolutionCore =
                content.Load<Texture2D>(
                    "Textures/evolution");


            HealthUpgrade =
                content.Load<Texture2D>(
                    "Textures/health");



            Acid =
                content.Load<Texture2D>(
                    "Textures/acid");


            Laser =
                content.Load<Texture2D>(
                    "Textures/laser");


            MovingPlatform =
                content.Load<Texture2D>(
                    "Textures/platform");


            Checkpoint =
                content.Load<Texture2D>(
                    "Textures/checkpoint");

            */

        }

    }
}