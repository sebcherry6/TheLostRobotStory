using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.World
{
    // 'M' moves sideways, 'I' is a lift, 'Q' crumbles:
    // a crumbling platform shakes when stood on, falls, and comes back later.
    public class MovingPlatform
    {
        private enum CrumbleState { Solid, Shaking, Falling, Gone }

        public Vector2 Position;
        private readonly Vector2 _startPosition;
        private readonly Vector2 _endPosition;

        private readonly float _speed;
        private bool _movingForward = true;

        // Movement delta this frame (the player rides along with it)
        public Vector2 Velocity { get; private set; }
        public bool IsMoving { get; private set; }

        public Rectangle Bounds;
        private readonly int _width;
        private readonly int _height;

        // crumbling
        public bool Crumbles { get; }
        private CrumbleState _crumble = CrumbleState.Solid;
        private float _crumbleTimer;
        private float _fallSpeed;
        private float _time;

        private const float ShakeTime = 0.45f;
        private const float RespawnTime = 3f;

        public MovingPlatform(Vector2 start, Vector2 end, int width = 96, int height = 16, float speed = 80f, bool crumbles = false)
        {
            Position = start;
            _startPosition = start;
            _endPosition = end;
            _width = width;
            _height = height;
            _speed = speed;
            Crumbles = crumbles;

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, _width, _height);
            Velocity = Vector2.Zero;
        }

        public static MovingPlatform Crumbling(Vector2 position) =>
            new MovingPlatform(position, position, 96, 16, 0f, true);

        // called by the player when it lands on this platform
        public void NotifyStoodOn()
        {
            if (!Crumbles || _crumble != CrumbleState.Solid)
                return;

            _crumble = CrumbleState.Shaking;
            _crumbleTimer = ShakeTime;
            AudioManager.Play("crumble", 0.7f);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            Vector2 oldPosition = Position;

            if (Crumbles)
                UpdateCrumble(dt);
            else
                UpdateMovement(dt);

            Velocity = Position - oldPosition;

            Bounds = _crumble == CrumbleState.Gone
                ? Rectangle.Empty
                : new Rectangle((int)Position.X, (int)Position.Y, _width, _height);
        }

        private void UpdateMovement(float dt)
        {
            Vector2 target = _movingForward ? _endPosition : _startPosition;
            Vector2 direction = target - Position;
            float distance = direction.Length();

            IsMoving = distance > 1f;

            if (!IsMoving)
                return;

            direction.Normalize();
            float movement = _speed * dt;

            if (movement >= distance)
            {
                Position = target;
                _movingForward = !_movingForward;
            }
            else
            {
                Position += direction * movement;
            }
        }

        private void UpdateCrumble(float dt)
        {
            switch (_crumble)
            {
                case CrumbleState.Shaking:
                    _crumbleTimer -= dt;

                    if (_crumbleTimer <= 0)
                    {
                        _crumble = CrumbleState.Falling;
                        _fallSpeed = 0f;
                    }
                    break;

                case CrumbleState.Falling:
                    _fallSpeed = Math.Min(_fallSpeed + 1400f * dt, 900f);
                    Position.Y += _fallSpeed * dt;

                    if (Position.Y > _startPosition.Y + 700)
                    {
                        _crumble = CrumbleState.Gone;
                        _crumbleTimer = RespawnTime;
                    }
                    break;

                case CrumbleState.Gone:
                    _crumbleTimer -= dt;

                    if (_crumbleTimer <= 0)
                    {
                        Position = _startPosition;
                        _crumble = CrumbleState.Solid;
                    }
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_crumble == CrumbleState.Gone)
                return;

            Rectangle destination = Bounds;

            if (_crumble == CrumbleState.Shaking)
                destination.X += (int)(_time * 60) % 2 == 0 ? -1 : 1;

            if (Crumbles)
            {
                Texture2D crumble = TextureManager.Get("crumble");
                int frame = _crumble == CrumbleState.Solid ? 0 : 1;

                if (crumble != null)
                {
                    Sprites.DrawFrame(spriteBatch, crumble, destination, frame, Color.White, 96);
                    return;
                }

                // fall back to the normal platform, rusty and cracked
                if (TextureManager.MovingPlatform != null)
                {
                    Sprites.DrawFrame(spriteBatch, TextureManager.MovingPlatform, destination, 0, frame == 0 ? new Color(220, 170, 140) : new Color(170, 110, 90), 96);
                    return;
                }

                spriteBatch.Draw(TextureManager.Pixel, destination, frame == 0 ? new Color(140, 100, 70) : new Color(110, 70, 50));
                return;
            }

            if (TextureManager.MovingPlatform != null)
            {
                Sprites.Draw(spriteBatch, TextureManager.MovingPlatform, destination, Color.White, frameWidth: 96);
                return;
            }

            spriteBatch.Draw(TextureManager.Pixel, destination, Color.DarkGray);
        }
    }
}
