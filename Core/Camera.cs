using Microsoft.Xna.Framework;
using System;

namespace TheLostRobotStory.Core
{
    public class Camera
    {

        public Vector2 Position { get; private set; }


        private readonly Vector2 _screenSize;


        private float _smoothSpeed = 8f;


        private float _zoom = 1f;


        private Rectangle? _worldBounds;



        private Vector2 _shakeOffset;


        private float _shakeTimer;


        private float _shakeStrength;



        public Camera(
            int screenWidth,
            int screenHeight)
        {

            _screenSize =
                new Vector2(
                    screenWidth,
                    screenHeight);


            Position =
                Vector2.Zero;

        }





        public void Follow(
            Vector2 targetPosition,
            GameTime gameTime)
        {

            Vector2 target =
                targetPosition -
                new Vector2(
                    _screenSize.X / 2,
                    _screenSize.Y / 2);



            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            Position =
                Vector2.Lerp(
                    Position,
                    -target,
                    _smoothSpeed * dt);



            ClampToWorld();

        }






        public void SetWorldBounds(
            Rectangle bounds)
        {

            _worldBounds =
                bounds;

        }






        private void ClampToWorld()
        {

            if (_worldBounds == null)
                return;



            Rectangle world =
                _worldBounds.Value;



            float maxX =
                world.Width -
                _screenSize.X;



            float maxY =
                world.Height -
                _screenSize.Y;



            Position =
                new Vector2(

                    MathHelper.Clamp(
                        Position.X,
                        -maxX,
                        0),


                    MathHelper.Clamp(
                        Position.Y,
                        -maxY,
                        0)

                );

        }








        public void Shake(
            float strength,
            float duration)
        {

            _shakeStrength =
                strength;


            _shakeTimer =
                duration;

        }





        public void UpdateShake(
            GameTime gameTime)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            if (_shakeTimer > 0)
            {

                _shakeTimer -= dt;



                _shakeOffset =
                    new Vector2(
                        Random.Shared.NextSingle() * 2 - 1,
                        Random.Shared.NextSingle() * 2 - 1)
                    *
                    _shakeStrength;

            }
            else
            {

                _shakeOffset =
                    Vector2.Zero;

            }

        }






        public void SetZoom(
            float zoom)
        {

            _zoom = zoom;

        }




        public void Reset(Vector2 position)
        {
            Position =
                -position +
                new Vector2(
                    _screenSize.X / 2,
                    _screenSize.Y / 2);
        }

        public Matrix GetViewMatrix()
        {

            return
                Matrix.CreateTranslation(
                    new Vector3(
                        Position +
                        _shakeOffset,
                        0))
                *
                Matrix.CreateScale(
                    _zoom);

        }

    }
}