using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.World
{
    public class MovingPlatform
    {

        // =====================================================
        // POSITION
        // =====================================================

        public Vector2 Position;


        private Vector2 _startPosition;

        private Vector2 _endPosition;



        // =====================================================
        // MOVEMENT
        // =====================================================

        private float _speed;


        private bool _movingForward = true;



        // Movement delta this frame
        public Vector2 Velocity { get; private set; }



        public bool IsMoving { get; private set; }




        // =====================================================
        // COLLISION
        // =====================================================

        public Rectangle Bounds;



        private int _width;

        private int _height;




        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public MovingPlatform(
            Vector2 start,
            Vector2 end,
            int width = 96,
            int height = 16,
            float speed = 80f)
        {

            Position =
                start;


            _startPosition =
                start;


            _endPosition =
                end;



            _width =
                width;


            _height =
                height;



            _speed =
                speed;



            Bounds =
                new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    _width,
                    _height);


            Velocity =
                Vector2.Zero;

        }








        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            Vector2 oldPosition =
                Position;



            Vector2 target =
                _movingForward
                ? _endPosition
                : _startPosition;



            Vector2 direction =
                target - Position;



            float distance =
                direction.Length();




            IsMoving =
                distance > 1f;



            if (IsMoving)
            {

                direction.Normalize();



                float movement =
                    _speed * dt;



                // prevent overshooting

                if (movement >= distance)
                {

                    Position =
                        target;



                    _movingForward =
                        !_movingForward;

                }
                else
                {

                    Position +=
                        direction *
                        movement;

                }

            }




            // How far platform moved this frame

            Velocity =
                Position -
                oldPosition;




            Bounds =
                new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    _width,
                    _height);

        }








        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch)
        {

            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                Color.DarkGray);

        }

    }
}