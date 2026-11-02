using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    // =========================================================
    // CONVEYOR BELT  ('<' moves left, '>' moves right)
    // Solid like a normal tile; anything standing on it is carried.
    // =========================================================
    public class Conveyor
    {
        public const float Speed = 70f;

        public Rectangle Bounds { get; }
        public int Direction { get; }

        public Conveyor(Rectangle bounds, int direction)
        {
            Bounds = bounds;
            Direction = direction;
        }

        public void Draw(SpriteBatch spriteBatch, bool stopped)
        {
            Texture2D texture = TextureManager.Get("conveyor");

            if (texture != null)
            {
                Sprites.Draw(spriteBatch, texture, Bounds, stopped ? Color.Gray : Color.White, frameWidth: 32, fps: stopped ? 0f : 7f,
                    effects: Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
                return;
            }

            spriteBatch.Draw(TextureManager.Pixel, Bounds, new Color(55, 58, 66));
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 6), new Color(35, 35, 40));

            // moving chevrons on the belt surface
            int offset = stopped ? 0 : (int)(Sprites.Time * Speed * 0.5f) % 16;

            if (Direction < 0)
                offset = 16 - offset;

            for (int x = -16 + offset; x < Bounds.Width; x += 16)
            {
                int px = Bounds.X + x;

                if (px < Bounds.X || px + 6 > Bounds.Right)
                    continue;

                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(px, Bounds.Y + 1, 6, 3), Color.Gold * 0.8f);
            }
        }
    }

    // =========================================================
    // CRUSHER  ('V', hangs under a ceiling, slams down on a cycle)
    // =========================================================
    public class Crusher
    {
        private enum State { Up, Warn, Down, Hold, Rise }

        private const int Width = 32;
        private const int HeadHeight = 24;

        private readonly int _x;
        private readonly int _topY;
        private readonly int _travel;

        private State _state = State.Up;
        private float _timer;
        private float _offset;
        private float _time;

        public bool Disabled;

        public Rectangle Head => new Rectangle(_x, _topY + (int)_offset, Width, HeadHeight);
        public bool IsDangerous => _state == State.Down || _state == State.Hold;

        public Crusher(Vector2 tilePosition, int travelPixels, float phaseOffset)
        {
            _x = (int)tilePosition.X;
            _topY = (int)tilePosition.Y;
            _travel = Math.Max(0, travelPixels);
            _timer = 1.2f + phaseOffset;
        }

        public void Update(float dt, Player player, List<Enemy> enemies, ParticleManager particles)
        {
            _time += dt;

            if (Disabled)
            {
                _state = State.Up;
                _offset = Math.Max(0, _offset - 120f * dt);
                return;
            }

            switch (_state)
            {
                case State.Up:
                    _timer -= dt;
                    if (_timer <= 0) { _state = State.Warn; _timer = 0.5f; }
                    break;

                case State.Warn:
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Down;
                    break;

                case State.Down:
                    _offset += 900f * dt;

                    if (_offset >= _travel)
                    {
                        _offset = _travel;
                        _state = State.Hold;
                        _timer = 0.35f;

                        float distance = Vector2.Distance(player.Center, Head.Center.ToVector2());

                        if (distance < 500)
                            AudioManager.Play("crusher", MathHelper.Clamp(1f - distance / 500f, 0.15f, 1f));

                        particles?.SpawnExplosion(new Vector2(Head.Center.X, Head.Bottom), Color.Gray);
                    }
                    break;

                case State.Hold:
                    _timer -= dt;
                    if (_timer <= 0) _state = State.Rise;
                    break;

                case State.Rise:
                    _offset -= 110f * dt;

                    if (_offset <= 0)
                    {
                        _offset = 0;
                        _state = State.Up;
                        _timer = 1.3f;
                    }
                    break;
            }

            if (!IsDangerous)
                return;

            Rectangle head = Head;

            if (PixelCollision.Overlaps(player.GetShape(), GetShape()))
                player.TakeDamage(1);

            // robots caught underneath get crushed too
            foreach (Enemy enemy in enemies)
            {
                if (!(enemy is Boss) && !enemy.IsDead && head.Intersects(enemy.Bounds))
                    enemy.TakeDamage(5);
            }
        }

        public SpriteShape GetShape()
        {
            Texture2D texture = TextureManager.Get("crusher");

            return texture == null
                ? SpriteShape.FromRect(Head)
                : SpriteShape.FromTexture(texture, Sprites.FrameRect(texture, _state == State.Warn ? 1 : 0, Width), Head);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Rectangle head = Head;

            // shaft
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(_x + 12, _topY, 8, (int)_offset + 2), new Color(70, 70, 78));

            Texture2D texture = TextureManager.Get("crusher");

            if (texture != null)
            {
                Sprites.DrawFrame(spriteBatch, texture, head, _state == State.Warn ? 1 : 0, Color.White, Width);
                return;
            }

            bool warn = _state == State.Warn && (int)(_time * 16) % 2 == 0;

            spriteBatch.Draw(TextureManager.Pixel, head, warn ? Color.Orange : new Color(120, 120, 130));

            // hazard stripes on the bottom edge
            for (int i = 0; i < 4; i++)
            {
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(head.X + i * 8, head.Bottom - 6, 4, 6), Color.Gold);
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(head.X + i * 8 + 4, head.Bottom - 6, 4, 6), Color.Black);
            }
        }
    }

    // =========================================================
    // POWER SWITCH  ('Z', press E to turn on)
    // =========================================================
    public class PowerSwitch
    {
        public Rectangle Bounds { get; }
        public string Key { get; }
        public bool On;
        public bool IsPlayerNear;

        private float _time;

        public PowerSwitch(Vector2 tilePosition, string key)
        {
            Bounds = new Rectangle((int)tilePosition.X + 4, (int)tilePosition.Y - 8, 24, 40);
            Key = key;
        }

        public Rectangle InteractArea
        {
            get
            {
                Rectangle area = Bounds;
                area.Inflate(16, 4);
                return area;
            }
        }

        public void Update(float dt) => _time += dt;

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureManager.Get("switch");

            if (texture != null)
            {
                Rectangle source = Sprites.FrameRect(texture, On ? 1 : 0, 24);

                if (IsPlayerNear && !On)
                    Sprites.DrawOutline(spriteBatch, texture, Bounds, source, Color.White * 0.8f);

                spriteBatch.Draw(texture, Bounds, source, Color.White);
                return;
            }

            spriteBatch.Draw(TextureManager.Pixel, Bounds, new Color(50, 55, 65));

            // lever
            Rectangle lever = On
                ? new Rectangle(Bounds.Center.X - 2, Bounds.Y + 6, 4, 14)
                : new Rectangle(Bounds.Center.X - 2, Bounds.Y + 20, 4, 14);

            spriteBatch.Draw(TextureManager.Pixel, lever, Color.LightGray);

            float blink = On ? 1f : 0.5f + 0.5f * (float)Math.Sin(_time * 6f);
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(Bounds.X + 4, Bounds.Y + 2, 6, 6), (On ? Color.LimeGreen : Color.Red) * blink);

            if (IsPlayerNear && !On)
            {
                Rectangle outline = Bounds;
                outline.Inflate(2, 2);
                spriteBatch.Draw(TextureManager.Pixel, outline, Color.White * 0.15f);
            }
        }
    }

    // =========================================================
    // RESEARCH DATA FRAGMENT  ('R')
    // =========================================================
    public class DataFragment
    {
        public Vector2 Position;
        public string Key { get; }
        public bool Collected;

        private float _time;

        public DataFragment(Vector2 tilePosition, string key)
        {
            Position = tilePosition + new Vector2(8, 8);
            Key = key;
        }

        public Rectangle Bounds =>
            new Rectangle((int)Position.X, (int)(Position.Y + Math.Sin(_time * 3f) * 4f), 16, 16);

        public void Update(float dt) => _time += dt;

        public static void DrawBeacon(SpriteBatch spriteBatch, Rectangle target, Color color, float time)
        {
            float pulse = 0.6f + 0.4f * (float)Math.Sin(time * 3f);
            int cx = target.Center.X;

            // light pillar fading upward
            for (int i = 0; i < 12; i++)
            {
                float alpha = (1f - i / 12f) * 0.22f * pulse;
                int width = 8 - i / 3;
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(cx - width / 2, target.Y - (i + 1) * 12, width, 12), color * alpha);
            }

            // three sparks circling the pickup
            for (int k = 0; k < 3; k++)
            {
                float a = time * 2.2f + k * MathHelper.TwoPi / 3f;
                int sx = cx + (int)(Math.Cos(a) * 15);
                int sy = target.Center.Y + (int)(Math.Sin(a) * 7);
                float twinkle = 0.5f + 0.5f * (float)Math.Sin(time * 9f + k);

                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(sx - 1, sy - 1, 2, 2), Color.White * twinkle);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Collected)
                return;

            Texture2D texture = TextureManager.Get("fragment");
            Rectangle b = Bounds;

            DrawBeacon(spriteBatch, b, new Color(170, 110, 255), _time);

            if (texture != null)
            {
                Rectangle source = Sprites.FrameRect(texture, Sprites.AnimFrame(texture, 16, 6f), 16);
                float outlinePulse = 0.6f + 0.4f * (float)Math.Sin(_time * 4f);

                Sprites.DrawOutline(spriteBatch, texture, b, source, new Color(200, 150, 255) * outlinePulse, SpriteEffects.None, 2);
                spriteBatch.Draw(texture, b, source, Color.White);
                return;
            }

            Rectangle glow = b;
            glow.Inflate(5, 5);

            float pulse = 0.5f + 0.3f * (float)Math.Sin(_time * 5f);
            spriteBatch.Draw(TextureManager.Pixel, glow, Color.MediumPurple * pulse);
            spriteBatch.Draw(TextureManager.Pixel, b, Color.Lavender);
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.X + 4, b.Y + 4, 8, 8), Color.MediumPurple);
        }
    }

    // =========================================================
    // BEACON: a soft light pillar + orbiting sparks so important
    // pickups can be spotted from a distance
    // =========================================================
    public static class Beacon
    {
        public static void Draw(SpriteBatch spriteBatch, Rectangle target, Color color, float time) =>
            DataFragment.DrawBeacon(spriteBatch, target, color, time);
    }

    // =========================================================
    // STORY TRIGGER  ('1'-'9', invisible zone)
    // =========================================================
    public class StoryTrigger
    {
        public Rectangle Bounds { get; }
        public string Number { get; }
        public string Id { get; }
        public bool Fired;

        public StoryTrigger(Vector2 tilePosition, string levelId, char number)
        {
            // a column 4 tiles tall ending at the marked tile
            Bounds = new Rectangle((int)tilePosition.X, (int)tilePosition.Y - 96, 32, 128);
            Number = number.ToString();
            Id = levelId + "_t" + number;
        }
    }

    // =========================================================
    // M-4, THE FORGOTTEN MACHINE  ('N', cage = 'J')
    // An old floating maintenance drone with a broken rotor.
    // =========================================================
    public class NpcM4
    {
        public enum NpcState { Idle, Following, Captured }

        public Vector2 Position;
        public NpcState State = NpcState.Idle;
        public Vector2? CagePosition;
        public bool IsPlayerNear;

        private float _time;
        private bool _facingLeft;

        public NpcM4(Vector2 position)
        {
            Position = position;
        }

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, 26, 26);

        public Rectangle InteractArea
        {
            get
            {
                Rectangle area = Bounds;
                area.Inflate(24, 16);
                return area;
            }
        }

        public void Update(float dt, Player player)
        {
            _time += dt;

            switch (State)
            {
                case NpcState.Following:
                    Vector2 target = player.position + new Vector2(-38 * player.FacingDirection, -34);
                    Vector2 previous = Position;
                    Position = Vector2.Lerp(Position, target, Math.Min(1f, 4f * dt));

                    if (Math.Abs(Position.X - previous.X) > 0.3f)
                        _facingLeft = Position.X < previous.X;
                    break;

                case NpcState.Captured:
                    if (CagePosition.HasValue)
                        Position = CagePosition.Value + new Vector2(3, 3);
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Rectangle b = Bounds;
            b.Y += (int)(Math.Sin(_time * 3f) * 3f);

            Texture2D texture = TextureManager.Get("m4");

            if (texture != null)
            {
                Rectangle source = Sprites.FrameRect(texture, Sprites.AnimFrame(texture, 26, 5f), 26);
                SpriteEffects flip = _facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                if (IsPlayerNear && State != NpcState.Following)
                    Sprites.DrawOutline(spriteBatch, texture, b, source, Color.Gold * 0.9f, flip);

                spriteBatch.Draw(texture, b, source, Color.White, 0f, Vector2.Zero, flip, 0f);
            }
            else
            {
                // rusty body, one yellow eye, crooked antenna, sputtering rotor
                spriteBatch.Draw(TextureManager.Pixel, b, new Color(150, 95, 55));
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.X + 4, b.Y + 8, 8, 6), Color.Gold);
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.Right - 8, b.Y - 7, 2, 7), Color.Gray);
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.Right - 12, b.Y - 8, 6, 2), Color.Gray);

                int rotor = (int)(_time * 20) % 2 == 0 ? 18 : 8;
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.Center.X - rotor / 2, b.Y - 3, rotor, 2), Color.LightGray);
            }

            if (State == NpcState.Captured && CagePosition.HasValue)
                DrawCage(spriteBatch, CagePosition.Value);

            if (IsPlayerNear && State != NpcState.Following && texture == null)
            {
                Rectangle outline = b;
                outline.Inflate(3, 3);
                spriteBatch.Draw(TextureManager.Pixel, outline, Color.White * 0.15f);
            }
        }

        public static void DrawCage(SpriteBatch spriteBatch, Vector2 cage)
        {
            Texture2D texture = TextureManager.Get("cage");
            Rectangle area = new Rectangle((int)cage.X - 4, (int)cage.Y - 4, 40, 40);

            if (texture != null)
            {
                spriteBatch.Draw(texture, area, Color.White);
                return;
            }

            for (int x = area.X; x <= area.Right; x += 8)
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, area.Y, 2, area.Height), new Color(140, 140, 150));

            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(area.X, area.Y, area.Width + 2, 3), new Color(140, 140, 150));
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(area.X, area.Bottom - 3, area.Width + 2, 3), new Color(140, 140, 150));
        }
    }
}
