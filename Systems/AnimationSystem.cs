using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheLostRobotStory.Systems
{
    public class AnimationSystem
    {

        // =====================================================
        // ANIMATION DATA
        // =====================================================

        private class Animation
        {

            public List<Rectangle> Frames =
                new();


            public float FrameTime;


            public bool Loop;


            public Animation(
                List<Rectangle> frames,
                float frameTime,
                bool loop = true)
            {

                Frames = frames;

                FrameTime = frameTime;

                Loop = loop;

            }

        }





        // =====================================================
        // VARIABLES
        // =====================================================

        private Dictionary<string, Animation> _animations =
            new();



        private string _currentAnimation;



        private int _currentFrame;



        private float _timer;



        private bool _finished;





        // =====================================================
        // CURRENT FRAME
        // =====================================================

        public Rectangle CurrentFrame
        {

            get
            {

                if (_currentAnimation == null)
                    return Rectangle.Empty;



                return _animations[_currentAnimation]
                    .Frames[_currentFrame];

            }

        }





        public bool Finished
        {
            get
            {
                return _finished;
            }
        }





        // =====================================================
        // ADD ANIMATION
        // =====================================================

        public void AddAnimation(
            string name,
            List<Rectangle> frames,
            float frameTime,
            bool loop = true)
        {

            _animations[name] =
                new Animation(
                    frames,
                    frameTime,
                    loop);

        }





        // =====================================================
        // PLAY ANIMATION
        // =====================================================

        public void Play(
            string name)
        {

            if (_currentAnimation == name)
                return;



            if (!_animations.ContainsKey(name))
                return;



            _currentAnimation =
                name;


            _currentFrame =
                0;


            _timer =
                0;


            _finished =
                false;

        }





        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime)
        {

            if (_currentAnimation == null)
                return;



            Animation animation =
                _animations[_currentAnimation];



            _timer +=
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            if (_timer >= animation.FrameTime)
            {

                _timer = 0;


                _currentFrame++;



                if (_currentFrame >= animation.Frames.Count)
                {

                    if (animation.Loop)
                    {

                        _currentFrame = 0;

                    }
                    else
                    {

                        _currentFrame =
                            animation.Frames.Count - 1;


                        _finished =
                            true;

                    }

                }

            }

        }





        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Vector2 position,
            SpriteEffects effects = SpriteEffects.None)
        {

            if (_currentAnimation == null)
                return;



            spriteBatch.Draw(
                texture,
                position,
                CurrentFrame,
                Color.White,
                0f,
                Vector2.Zero,
                1f,
                effects,
                0f);

        }





        // =====================================================
        // RESET
        // =====================================================

        public void Reset()
        {

            _currentFrame = 0;

            _timer = 0;

            _finished = false;

        }

    }
}