using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    // Chapter 3 boss: a prototype combat robot that ADAPTS.
    // If most recent hits came from one attack type (melee or energy),
    // it raises a shield against that type for a few seconds.
    // The player has to keep switching between strike and shoot.
    public class GuardianBoss : Boss
    {
        private enum Mode { Walk, Burst, Windup, Charge, Leap, Recover }

        private Mode _mode = Mode.Walk;
        private float _modeTimer = 2f;
        private int _burstLeft;
        private float _burstTimer;

        private readonly Queue<bool> _recentHits = new(); // true = ranged
        private bool _shieldRanged;
        private bool _shieldMelee;
        private float _shieldTimer;

        public GuardianBoss(Vector2 startPos) : base(startPos, 26, new Vector2(64, 64))
        {
            Name = "PROTOTYPE G-0";
            StoryId = "guardian";
        }

        public override string Status =>
            _shieldMelee ? "ADAPTED: MELEE RESISTANT - USE ENERGY" :
            _shieldRanged ? "ADAPTED: ENERGY RESISTANT - USE MELEE" :
            "";

        public override bool TakeHit(int damage, bool ranged, int fromSide)
        {
            if (!Activated)
                ForceActivate();

            if ((ranged && _shieldRanged) || (!ranged && _shieldMelee))
            {
                AudioManager.Play("block");
                return false;
            }

            _recentHits.Enqueue(ranged);

            while (_recentHits.Count > 4)
                _recentHits.Dequeue();

            TakeDamage(damage);

            if (!_shieldRanged && !_shieldMelee && _recentHits.Count >= 4)
            {
                int rangedHits = _recentHits.Count(h => h);

                if (rangedHits >= 3)
                    Adapt(true);
                else if (rangedHits <= 1)
                    Adapt(false);
            }

            return true;
        }

        private void Adapt(bool againstRanged)
        {
            _shieldRanged = againstRanged;
            _shieldMelee = !againstRanged;
            _shieldTimer = Phase == 3 ? 7f : 5f;
            _recentHits.Clear();

            AudioManager.Play("adapt");
        }

        private float WalkSpeed => Phase == 1 ? 58f : Phase == 2 ? 78f : 98f;
        private float NextDelay => Phase == 1 ? 2.2f : Phase == 2 ? 1.7f : 1.2f;

        protected override void UpdateBoss(List<Rectangle> solids, Player player, List<Projectile> projectiles, float dt)
        {
            if (_shieldTimer > 0)
            {
                _shieldTimer -= dt;

                if (_shieldTimer <= 0)
                {
                    _shieldRanged = false;
                    _shieldMelee = false;
                }
            }

            float dx = player.Center.X - Center.X;

            switch (_mode)
            {
                case Mode.Walk:
                    if (Math.Abs(dx) > 8)
                        _direction = Math.Sign(dx);

                    _moving = Math.Abs(dx) > 48;

                    if (_moving)
                        MoveHorizontal(solids, _direction * WalkSpeed * dt);

                    FallAndCollide(solids, dt);

                    _modeTimer -= dt;

                    if (_modeTimer <= 0)
                        ChooseAttack(dx);
                    break;

                case Mode.Burst:
                    FallAndCollide(solids, dt);
                    _burstTimer -= dt;

                    if (_burstTimer <= 0 && _burstLeft > 0)
                    {
                        FireAt(player, projectiles, 235f);
                        _sinceShot = 0f;
                        AudioManager.Play("shoot", 0.5f, 0.2f);
                        _burstLeft--;
                        _burstTimer = 0.18f;
                    }

                    if (_burstLeft == 0)
                        EndAttack(0.3f);
                    break;

                case Mode.Windup:
                    FallAndCollide(solids, dt);
                    _modeTimer -= dt;

                    if (_modeTimer <= 0)
                    {
                        _mode = Mode.Charge;
                        _modeTimer = 0.8f;
                        ContactDamage = 2;
                        AudioManager.Play("boss_dash");
                    }
                    break;

                case Mode.Charge:
                    _modeTimer -= dt;

                    bool hitWall = MoveHorizontal(solids, _direction * 340f * dt);
                    FallAndCollide(solids, dt);

                    if (hitWall || _modeTimer <= 0)
                    {
                        ContactDamage = 1;

                        if (hitWall)
                        {
                            BossSlamEvent?.Invoke();
                            AudioManager.Play("crusher");
                        }

                        EndAttack(hitWall ? 1.2f : 0.4f); // stuck in the wall = opening
                    }
                    break;

                case Mode.Leap:
                    MoveHorizontal(solids, velocity.X * dt);
                    FallAndCollide(solids, dt);

                    if (_grounded)
                    {
                        velocity.X = 0;
                        _sinceLanding = 0f;
                        SlamShockwaves(projectiles, 205f);
                        AudioManager.Play("boss_slam");
                        EndAttack(0.6f);
                    }
                    break;

                case Mode.Recover:
                    FallAndCollide(solids, dt);
                    _modeTimer -= dt;

                    if (_modeTimer <= 0)
                    {
                        _mode = Mode.Walk;
                        _modeTimer = NextDelay;
                    }
                    break;
            }
        }

        private void ChooseAttack(float dx)
        {
            _attackElapsed = 0f;

            int roll = Random.Shared.Next(Phase >= 2 ? 3 : 2);

            if (Math.Abs(dx) > 8)
                _direction = Math.Sign(dx);

            switch (roll)
            {
                case 0:
                    _mode = Mode.Burst;
                    _burstLeft = Phase == 3 ? 5 : 3;
                    _burstTimer = 0.25f;
                    break;

                case 1:
                    _mode = Mode.Windup;
                    _modeTimer = 0.55f;
                    break;

                default:
                    _mode = Mode.Leap;
                    velocity.Y = -760f;
                    velocity.X = MathHelper.Clamp(dx * 1.1f, -320f, 320f);
                    _grounded = false;
                    break;
            }
        }

        private void EndAttack(float recover)
        {
            _mode = Mode.Recover;
            _modeTimer = recover;
        }

        // guardian.png: row 0 walk, row 1 leap (thrusters), row 2 attack (frame 3 = landing dust)
        private Rectangle CurrentSource(Texture2D texture)
        {
            switch (_mode)
            {
                case Mode.Leap:
                    return SheetFrame(texture, 1, AirFrame());

                case Mode.Windup:
                    return SheetFrame(texture, 2, (int)(_time * 8f) % 2);

                case Mode.Charge:
                    return SheetFrame(texture, 2, 1 + (int)(_time * 12f) % 2);

                case Mode.Burst:
                    return SheetFrame(texture, 2, _sinceShot < 0.12f ? 2 : 1);

                case Mode.Recover:
                    return _sinceLanding < 0.4f
                        ? SheetFrame(texture, 2, 3)
                        : SheetFrame(texture, 0, 0);

                default:
                    return SheetFrame(texture, 0, _moving ? LoopFrame(texture, 6f) : 0);
            }
        }

        public override SpriteShape GetShape()
        {
            Texture2D texture = TextureManager.Get("guardian");

            if (texture == null)
                return SpriteShape.FromRect(Bounds);

            return SpriteShape.FromTexture(texture, CurrentSource(texture), Bounds, _direction < 0);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead || Despawned)
                return;

            Texture2D texture = TextureManager.Get("guardian");
            Rectangle b = Bounds;

            if ((_shieldMelee || _shieldRanged) && texture == null)
            {
                Rectangle aura = b;
                aura.Inflate(6, 6);
                float pulse = 0.4f + 0.2f * (float)Math.Sin(_time * 10f);
                spriteBatch.Draw(TextureManager.Pixel, aura, (_shieldMelee ? Color.Orange : Color.Cyan) * pulse);
            }

            bool windup = _mode == Mode.Windup && (int)(_time * 16) % 2 == 0;

            if (texture != null)
            {
                Color tint = _damagedFlash > 0 ? Color.Red : windup ? Color.Yellow : Color.White;
                SpriteEffects flip = _direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Rectangle source = CurrentSource(texture);

                // adaptive shield hugs the robot's outline (otherwise the boss highlight)
                if (_shieldMelee || _shieldRanged)
                {
                    float pulse = 0.6f + 0.4f * (float)Math.Sin(_time * 10f);
                    Sprites.DrawOutline(spriteBatch, texture, b, source, (_shieldMelee ? Color.Orange : Color.Cyan) * pulse, flip, 3);
                }
                else
                {
                    DrawHighlight(spriteBatch, texture, b, source, flip);
                }

                spriteBatch.Draw(texture, b, source, tint, 0f, Vector2.Zero, flip, 0f);
                return;
            }

            spriteBatch.Draw(TextureManager.Pixel, b, FlashOr(windup ? Color.Yellow : new Color(70, 110, 150)));
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(b.X + 8, b.Y + 30, b.Width - 16, 6), new Color(40, 60, 80));
            DrawEye(spriteBatch, Phase == 3 ? Color.Red : Color.Cyan, 14);
        }
    }
}
