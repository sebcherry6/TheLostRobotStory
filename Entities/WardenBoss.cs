using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    // WARDEN-7: the hunter. Silent, relentless.
    // cameo = true  -> a short encounter (Chapter 2): it retreats after
    //                  taking enough damage or after ~30 seconds.
    // cameo = false -> the full fight in Chapter 4.
    public class WardenBoss : Boss
    {
        private enum Mode { Stalk, Windup, Charge, Burst, Pounce, Recover }

        private readonly bool _cameo;
        private readonly int _retreatHealth;
        private float _cameoTimer = 30f;

        private Mode _mode = Mode.Stalk;
        private float _modeTimer = 1.5f;
        private int _burstLeft;
        private float _burstTimer;

        public WardenBoss(Vector2 startPos, bool cameo)
            : base(startPos, cameo ? 30 : 36, new Vector2(40, 64))
        {
            _cameo = cameo;
            _retreatHealth = MaxHealth - 10;

            Name = "WARDEN-7";
            StoryId = cameo ? "warden_cameo" : "warden";

            if (cameo)
                ForceActivate();
        }

        public override string Status => _cameo ? "HUNTER UNIT - SURVIVE" : "";

        private float StalkSpeed => Phase == 1 ? 78f : Phase == 2 ? 98f : 122f;
        private float NextDelay => Phase == 1 ? 1.8f : Phase == 2 ? 1.4f : 1.0f;

        protected override void UpdateBoss(List<Rectangle> solids, Player player, List<Projectile> projectiles, float dt)
        {
            if (_cameo)
            {
                _cameoTimer -= dt;

                if (Health <= _retreatHealth || _cameoTimer <= 0)
                {
                    Despawned = true;
                    return;
                }
            }

            float dx = player.Center.X - Center.X;

            switch (_mode)
            {
                case Mode.Stalk:
                    if (Math.Abs(dx) > 8)
                        _direction = Math.Sign(dx);

                    _moving = Math.Abs(dx) > 36;

                    if (_moving)
                        MoveHorizontal(solids, _direction * StalkSpeed * dt);

                    FallAndCollide(solids, dt);

                    _modeTimer -= dt;

                    if (_modeTimer <= 0)
                        ChooseAttack(dx);
                    break;

                case Mode.Windup:
                    FallAndCollide(solids, dt);
                    _modeTimer -= dt;

                    if (_modeTimer <= 0)
                    {
                        _mode = Mode.Charge;
                        _modeTimer = 0.7f;
                        ContactDamage = 2;
                        AudioManager.Play("boss_dash");
                    }
                    break;

                case Mode.Charge:
                    _modeTimer -= dt;

                    bool hitWall = MoveHorizontal(solids, _direction * 390f * dt);
                    FallAndCollide(solids, dt);

                    if (hitWall || _modeTimer <= 0)
                    {
                        ContactDamage = 1;
                        Recover(hitWall ? 1.0f : 0.35f);
                    }
                    break;

                case Mode.Burst:
                    FallAndCollide(solids, dt);
                    _burstTimer -= dt;

                    if (_burstTimer <= 0 && _burstLeft > 0)
                    {
                        FireAt(player, projectiles, 270f);
                        _sinceShot = 0f;
                        AudioManager.Play("shoot", 0.5f, 0.2f);
                        _burstLeft--;
                        _burstTimer = 0.12f;
                    }

                    if (_burstLeft == 0)
                        Recover(0.3f);
                    break;

                case Mode.Pounce:
                    MoveHorizontal(solids, velocity.X * dt);
                    FallAndCollide(solids, dt);

                    if (_grounded)
                    {
                        velocity.X = 0;
                        _sinceLanding = 0f;
                        SlamShockwaves(projectiles, Phase == 3 ? 255f : 210f);
                        AudioManager.Play("boss_slam");
                        Recover(0.5f);
                    }
                    break;

                case Mode.Recover:
                    FallAndCollide(solids, dt);
                    _modeTimer -= dt;

                    if (_modeTimer <= 0)
                    {
                        _mode = Mode.Stalk;
                        _modeTimer = NextDelay;
                    }
                    break;
            }
        }

        private void ChooseAttack(float dx)
        {
            _attackElapsed = 0f;

            if (Math.Abs(dx) > 8)
                _direction = Math.Sign(dx);

            int options = Phase >= 2 || _cameo ? 3 : 2;

            switch (Random.Shared.Next(options))
            {
                case 0:
                    _mode = Mode.Windup;
                    _modeTimer = 0.45f;
                    break;

                case 1:
                    _mode = Mode.Burst;
                    _burstLeft = 3;
                    _burstTimer = 0.2f;
                    break;

                default:
                    _mode = Mode.Pounce;
                    velocity.Y = -720f;
                    velocity.X = MathHelper.Clamp(dx * 1.2f, -350f, 350f);
                    _grounded = false;
                    break;
            }
        }

        private void Recover(float time)
        {
            _mode = Mode.Recover;
            _modeTimer = time;
        }

        // warden.png: row 0 stalk, row 1 lunge (charge / pounce), row 2 claw strike
        private Rectangle CurrentSource(Texture2D texture)
        {
            switch (_mode)
            {
                case Mode.Windup:
                    return SheetFrame(texture, 1, 0);

                case Mode.Charge:
                    return SheetFrame(texture, 1, 1 + (int)(_time * 12f) % 3);

                case Mode.Pounce:
                    return SheetFrame(texture, 1, AirFrame());

                case Mode.Burst:
                    // the strike plays through once per shot
                    return SheetFrame(texture, 2, Math.Min(3, (int)(_sinceShot / 0.04f)));

                case Mode.Recover:
                    return _sinceLanding < 0.3f
                        ? SheetFrame(texture, 2, 3)
                        : SheetFrame(texture, 0, 0);

                default:
                    return SheetFrame(texture, 0, _moving ? LoopFrame(texture, 6f) : 0);
            }
        }

        public override SpriteShape GetShape()
        {
            Texture2D texture = TextureManager.Get("warden");

            if (texture == null)
                return SpriteShape.FromRect(Bounds);

            return SpriteShape.FromTexture(texture, CurrentSource(texture), Bounds, _direction < 0);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead || Despawned)
                return;

            Rectangle b = Bounds;
            bool windup = _mode == Mode.Windup && (int)(_time * 16) % 2 == 0;
            Texture2D texture = TextureManager.Get("warden");

            if (texture != null)
            {
                Color tint = _damagedFlash > 0 ? Color.Red : windup ? Color.Yellow : Color.White;
                SpriteEffects flip = _direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Rectangle source = CurrentSource(texture);

                DrawHighlight(spriteBatch, texture, b, source, flip);
                spriteBatch.Draw(texture, b, source, tint, 0f, Vector2.Zero, flip, 0f);
                return;
            }

            spriteBatch.Draw(TextureManager.Pixel, b, FlashOr(windup ? Color.Yellow : new Color(45, 45, 55)));

            // armour plates
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.X + 4, b.Y + 30, b.Width - 8, 4), new Color(90, 90, 100));
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.X + 4, b.Y + 44, b.Width - 8, 4), new Color(90, 90, 100));

            // single red visor
            int eyeX = _direction > 0 ? b.Right - 16 : b.X + 4;
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(eyeX, b.Y + 10, 12, 4), Color.Red);
        }
    }
}
