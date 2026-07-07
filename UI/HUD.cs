using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory
{
    public class HUD
    {

        private readonly SpriteFont _font;



        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public HUD(SpriteFont font)
        {
            _font = font;
        }





        // =====================================================
        // DRAW HUD
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch,
            Player player,
            int collectiblesRemaining,
            int enemiesRemaining)
        {


            if (_font == null)
                return;



            // =================================================
            // HEALTH
            // =================================================

            spriteBatch.DrawString(
                _font,
                "Health:",
                new Vector2(20, 15),
                Color.White);



            for (int i = 0; i < player.MaxHealth; i++)
            {

                spriteBatch.Draw(
                    TextureManager.Pixel,

                    new Rectangle(
                        90 + (i * 25),
                        15,
                        18,
                        18),

                    i < player.Health
                    ? Color.Red
                    : Color.DarkRed);

            }





            // =================================================
            // ENERGY CRYSTALS
            // =================================================

            spriteBatch.DrawString(
                _font,

                "Crystals left: "
                + collectiblesRemaining,

                new Vector2(
                    20,
                    55),

                Color.Gold);






            // =================================================
            // ENEMIES
            // =================================================

            spriteBatch.DrawString(
                _font,

                "Enemies left: "
                + enemiesRemaining,

                new Vector2(
                    20,
                    85),

                Color.White);







            // =================================================
            // EVOLUTION
            // =================================================

            string evolutionText;



            switch (player.EvolutionStage)
            {

                case 0:

                    evolutionText =
                        "Evolution: Basic Robot";

                    break;



                case 1:

                    evolutionText =
                        "Evolution: Stage 1 - Shooter";

                    break;



                case 2:

                    evolutionText =
                        "Evolution: Stage 2 - Double Jump";

                    break;



                case 3:

                    evolutionText =
                        "Evolution: Final Evolution";

                    break;



                default:

                    evolutionText =
                        "Evolution: Unknown";

                    break;

            }




            spriteBatch.DrawString(
                _font,

                evolutionText,

                new Vector2(
                    20,
                    115),

                Color.Cyan);



        }

    }
}