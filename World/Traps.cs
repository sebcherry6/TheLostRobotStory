using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    // =========================================================
    // SPIKES  ('^', on the floor)  spikes.png = 32x32
    // Hurts on pixel contact and bounces you off.
    // =========================================================
    public class Spikes
    {
        public Rectangle Bounds { get; }

        public Spikes(Vector2 tilePosition)
        {
            Bounds = new Rectangle((int)tilePosition.X, (int)tilePosition.Y, 32, 32);
        }

        public SpriteShape GetShape()
        {
            Texture2D texture = TextureManager.Get("spikes");

            return texture != null
                ? SpriteShape.FromTexture(texture, Sprites.FrameRect(texture, 0, 32), Bounds)
                : SpriteShape.FromRect(new Rectangle(Bounds.X + 2, Bounds.Y + 18, Bounds.Width - 4, 14));
        }

        public void Update(Player player)
        {
            if (PixelCollision.Overlaps(player.GetShape(), GetShape()))
            {
                player.TakeDamage(1);
                player.velocity.Y = -460f;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureManager.Get("spikes");

            if (texture != null)
            {
                Sprites.DrawFrame(spriteBatch, texture, Bounds, 0, Color.White, 32);
                return;
            }

            // placeholder: four steel teeth on a base plate
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(Bounds.X, Bounds.Bottom - 4, 32, 4), new Color(70, 70, 80));

            for (int i = 0; i < 4; i++)
            {
                int x = Bounds.X + i * 8;

                for (int row = 0; row < 12; row++)
                {
                    int half = (row + 1) / 3;
                    spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x + 4 - half, Bounds.Bottom - 16 + row, half * 2, 1), Color.Silver);
                }
            }
        }
    }

    // =========================================================
    // FLAME VENT  ('f', nozzle on the floor, fire 3 tiles high)
    // flame_vent.png = 4 frames of 32x96: 0 off, 1 warning, 2-3 fire
    // =========================================================
    public class FlameVent
    {
        private enum State { Off, Warn, On }

        private const float OffTime = 2.2f;
        private const float WarnTime = 0.7f;
        private const float OnTime = 1.3f;

        private readonly Rectangle _area;   // whole 32x96 sprite area
        private State _state = State.Off;
        private float _timer;
        private float _time;
        private float _particleTimer;

        public FlameVent(Vector2 tilePosition, float phaseOffset)
        {
            // the vent tile is the bottom of the column, fire rises two tiles above it
            _area = new Rectangle((int)tilePosition.X, (int)tilePosition.Y - 64, 32, 96);
            _timer = OffTime * 0.5f + phaseOffset;
        }

        public bool IsBurning => _state == State.On;

        private int Frame =>
            _state == State.Off ? 0 :
            _state == State.Warn ? 1 :
            2 + (int)(_time * 9) % 2;

        public SpriteShape GetShape()
        {
            Texture2D texture = TextureManager.Get("flame_vent");

            return texture != null
                ? SpriteShape.FromTexture(texture, Sprites.FrameRect(texture, Frame, 32), _area)
                : SpriteShape.FromRect(new Rectangle(_area.X + 6, _area.Y + 4, 20, _area.Height - 12));
        }

        public void Update(float dt, Player player, ParticleManager particles)
        {
            _time += dt;
            _timer -= dt;

            if (_timer <= 0)
            {
                switch (_state)
                {
                    case State.Off:
                        _state = State.Warn;
                        _timer = WarnTime;
                        break;

                    case State.Warn:
                        _state = State.On;
                        _timer = OnTime;

                        if (Vector2.Distance(player.Center, _area.Center.ToVector2()) < 500)
                            AudioManager.Play("flame", 0.6f);
                        break;

                    default:
                        _state = State.Off;
                        _timer = OffTime;
                        break;
                }
            }

            if (_state != State.On)
                return;

            _particleTimer -= dt;

            if (_particleTimer <= 0)
            {
                _particleTimer = 0.12f;
                particles?.SpawnBubble(new Vector2(_area.Center.X, _area.Y + 8), Color.Orange);
            }

            if (PixelCollision.Overlaps(player.GetShape(), GetShape()))
                player.TakeDamage(1);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureManager.Get("flame_vent");

            if (texture != null)
            {
                Sprites.DrawFrame(spriteBatch, texture, _area, Frame, Color.White, 32);
                return;
            }

            // placeholder nozzle + flame column
            Rectangle nozzle = new Rectangle(_area.X + 4, _area.Bottom - 8, 24, 8);
            spriteBatch.Draw(TextureManager.Pixel, nozzle, _state == State.Warn && (int)(_time * 12) % 2 == 0 ? Color.Orange : new Color(80, 80, 90));

            if (_state == State.On)
            {
                float flicker = 0.75f + 0.25f * (float)Math.Sin(_time * 40f);
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(_area.X + 6, _area.Y + 4, 20, _area.Height - 12), Color.OrangeRed * flicker);
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(_area.X + 11, _area.Y + 20, 10, _area.Height - 28), Color.Yellow * flicker);
            }
        }
    }
}
