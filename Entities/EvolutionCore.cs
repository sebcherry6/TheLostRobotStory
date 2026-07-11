using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class EvolutionCore
    {

        // =====================================================
        // DATA
        // =====================================================

        public Vector2 Position;

        public bool Collected;

        public int TargetEvolutionStage;



        // =====================================================
        // ANIMATION
        // =====================================================

        private float _time;

        private const int Size = 32;



        // =====================================================
        // COLLISION
        // =====================================================

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    Size,
                    Size);
            }
        }





        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public EvolutionCore(
            Vector2 position,
            int targetStage)
        {

            Position =
                position;


            TargetEvolutionStage =
                targetStage;


            Collected =
                false;

        }





        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime)
        {

            if (Collected)
                return;



            _time +=
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;

        }







        // =====================================================
        // COLLECT
        // =====================================================

        public void Collect(
            Player player)
        {

            if (Collected)
                return;



            Collected = true;



            // Only evolve if higher stage

            if (player.EvolutionStage < TargetEvolutionStage)
            {

                player.Evolve(
                    TargetEvolutionStage);



                // Restore health after evolution

                player.Health =
                    player.MaxHealth;

            }

        }








        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            if (Collected)
                return;



            float floatOffset =
                (float)Math.Sin(
                    _time * 3f)
                    * 5f;



            float pulse =
                0.7f +
                (float)Math.Sin(
                    _time * 6f)
                    * 0.2f;





            Rectangle glow =
                new Rectangle(
                    (int)Position.X - 8,
                    (int)(Position.Y + floatOffset) - 8,
                    Size + 16,
                    Size + 16);





            Rectangle core =
                new Rectangle(
                    (int)Position.X,
                    (int)(Position.Y + floatOffset),
                    Size,
                    Size);





            // Outer glow

            spriteBatch.Draw(
                TextureManager.Pixel,
                glow,
                Color.Cyan * pulse);





            // Core

            spriteBatch.Draw(
                TextureManager.Pixel,
                core,
                Color.DeepSkyBlue);





            // Centre light

            Rectangle centre =
                new Rectangle(
                    core.X + 8,
                    core.Y + 8,
                    16,
                    16);



            spriteBatch.Draw(
                TextureManager.Pixel,
                centre,
                Color.White);

        }

    }
}