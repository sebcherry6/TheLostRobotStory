using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    // Final boss: THE CORE in a combat body built from the facility's last power.
    // Phase 1: aimed energy orbs + reconnection pull
    // Phase 2: + sweeping laser, summoned security drones
    // Phase 3: + heavy ground slams with shockwaves (opening afterwards)
    public class CoreBoss : Boss
    {
        private enum BeamState { Off, Telegraph, Sweep }
        private enum SlamState { None, Rise, Dive, Stuck }

        private Vector2 _flyVelocity;

        private float _shotTimer = 1.6f;
        private float _sweepTimer = 4f;
        private float _summonTimer = 6f;
        private float _slamTimer = 5f;

        // laser sweep
        private BeamState _beam = BeamState.Off;
        private float _beamTimer;
        private float _beamFrom;
        private float _beamTo;
        private float _beamAngle;
        private float _beamLength;
        private const float BeamMaxLength = 460f;
        private const float TelegraphTime = 1.1f;
        private const float SweepTime = 2.0f;

        // ground slam
        private SlamState _slam = SlamState.None;
        private float _slamStateTimer;

        private readonly List<Enemy> _drones = new();

        protected override bool Flying => true;

        public CoreBoss(Vector2 startPos) : base(startPos, 48, new Vector2(96, 96))
        {
            Name = "THE CORE";
            StoryId = "core";
            ActivationRange = 560f;
        }

        public override string Status =>
            _slam == SlamState.Stuck ? "CORE EXPOSED - ATTACK NOW" : "";

        // more damage while it is stuck after a slam
        public override bool TakeHit(int damage, bool ranged, int fromSide)
        {
            if (!Activated)
                ForceActivate();

            TakeDamage(_slam == SlamState.Stuck ? damage * 2 : damage);
            return true;
        }

        protected override void UpdateBoss(List<Rectangle> solids, Player player, List<Projectile> projectiles, float dt)
        {
            _drones.RemoveAll(d => d.IsDead || d.Despawned);

            if (_slam != SlamState.None)
            {
                UpdateSlam(solids, projectiles, dt);
                return;
            }

            if (_beam != BeamState.Off)
            {
                UpdateBeam(player, dt);
                Hover(player, dt, 0.25f);
                return;
            }

            Hover(player, dt, 1f);

            if (Phase >= 2)
                PullPlayer(player, dt);

            // ---- timers
            _shotTimer -= dt;
            _sweepTimer -= dt;
            _summonTimer -= dt;
            _slamTimer -= dt;

            if (_shotTimer <= 0)
            {
                _shotTimer = Phase == 1 ? 1.4f : Phase == 2 ? 1.0f : 0.75f;

                FireAt(player, projectiles, 185f, ProjectileKind.Orb);
                _sinceShot = 0f;

                if (Phase >= 2)
                {
                    FireAt(player, projectiles, 185f, ProjectileKind.Orb, 0.3f);
                    FireAt(player, projectiles, 185f, ProjectileKind.Orb, -0.3f);
                }

                AudioManager.Play("shoot", 0.6f, 0.05f);
            }

            if (Phase >= 2 && _sweepTimer <= 0)
            {
                _sweepTimer = Phase == 2 ? 8f : 6.5f;
                StartSweep(player);
                return;
            }

            if (Phase >= 2 && _summonTimer <= 0)
            {
                _summonTimer = 10f;

                if (_drones.Count < 3)
                {
                    SummonDrones();
                    _attackElapsed = 0f;
                }
            }

            if (Phase == 3 && _slamTimer <= 0)
            {
                _slamTimer = 6.5f;
                _slam = SlamState.Rise;
                _slamStateTimer = 0.5f;
                AudioManager.Play("boss_dash");
            }
        }

        // =====================================================
        // MOVEMENT
        // =====================================================

        private void Hover(Player player, float dt, float speedScale)
        {
            float hoverY = Arena != Rectangle.Empty ? Arena.Top + 70 : player.position.Y - 180;

            Vector2 target = new Vector2(player.Center.X - size.X / 2f, hoverY);
            Vector2 desired = target - position;

            if (desired.LengthSquared() > 1)
                desired = Vector2.Normalize(desired) * (Phase == 3 ? 145f : 110f) * speedScale;

            _flyVelocity = Vector2.Lerp(_flyVelocity, desired, 2f * dt);
            position += _flyVelocity * dt;
            position.Y += (float)Math.Sin(_time * 3f) * 25f * dt;

            _direction = player.Center.X < Center.X ? -1 : 1;

            ClampToArena();
        }

        // "reconnection field": a gentle pull toward the core
        private void PullPlayer(Player player, float dt)
        {
            Vector2 pull = Center - player.Center;
            float distance = pull.Length();

            if (distance > 40 && distance < 380)
            {
                pull.Normalize();
                player.position.X += pull.X * 55f * dt;
            }
        }

        // =====================================================
        // LASER SWEEP
        // =====================================================

        private void StartSweep(Player player)
        {
            _beam = BeamState.Telegraph;
            _beamTimer = TelegraphTime;

            // sweep across the floor, starting on the player's side
            bool fromLeft = player.Center.X < Center.X;

            _beamFrom = MathHelper.ToRadians(fromLeft ? 165f : 15f);
            _beamTo = MathHelper.ToRadians(fromLeft ? 15f : 165f);
            _beamAngle = _beamFrom;

            AudioManager.Play("laser_beam");
        }

        private void UpdateBeam(Player player, float dt)
        {
            _beamTimer -= dt;
            _beamLength = MeasureBeam(_beamAngle);

            if (_beam == BeamState.Telegraph)
            {
                if (_beamTimer <= 0)
                {
                    _beam = BeamState.Sweep;
                    _beamTimer = SweepTime;
                }

                return;
            }

            float t = 1f - Math.Max(0f, _beamTimer) / SweepTime;
            _beamAngle = MathHelper.Lerp(_beamFrom, _beamTo, t);
            _beamLength = MeasureBeam(_beamAngle);

            Vector2 direction = new Vector2((float)Math.Cos(_beamAngle), (float)Math.Sin(_beamAngle));
            Vector2 end = Center + direction * _beamLength;

            if (DistanceToSegment(player.Center, Center, end) < 18f)
                player.TakeDamage(1);

            if (_beamTimer <= 0)
                _beam = BeamState.Off;
        }

        private float MeasureBeam(float angle)
        {
            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

            for (float d = size.X / 2f; d < BeamMaxLength; d += 12f)
            {
                if (SolidQuery != null && SolidQuery(Center + direction * d))
                    return d;
            }

            return BeamMaxLength;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.LengthSquared();

            if (lengthSquared < 0.001f)
                return Vector2.Distance(p, a);

            float t = MathHelper.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }

        // =====================================================
        // DRONES
        // =====================================================

        private void SummonDrones()
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Enemy drone = new Enemy(Center + new Vector2(i * 70, 20), EnemyType.Drone);
                _drones.Add(drone);
                SpawnedEnemies.Add(drone);
            }

            AudioManager.Play("terminal");
        }

        // =====================================================
        // GROUND SLAM
        // =====================================================

        private void UpdateSlam(List<Rectangle> solids, List<Projectile> projectiles, float dt)
        {
            switch (_slam)
            {
                case SlamState.Rise:
                    position.Y -= 140f * dt;
                    ClampToArena();
                    _slamStateTimer -= dt;

                    if (_slamStateTimer <= 0)
                        _slam = SlamState.Dive;
                    break;

                case SlamState.Dive:
                    position.Y += 680f * dt;

                    foreach (Rectangle tile in solids)
                    {
                        if (!Bounds.Intersects(tile))
                            continue;

                        position.Y = tile.Top - size.Y;

                        SlamShockwaves(projectiles, 280f);
                        projectiles.Add(Projectile.Shockwave(Bounds.Left - 8, Bounds.Bottom, -1, 170f));
                        projectiles.Add(Projectile.Shockwave(Bounds.Right + 8, Bounds.Bottom, 1, 170f));

                        AudioManager.Play("boss_slam");

                        _slam = SlamState.Stuck;
                        _slamStateTimer = 1.6f;
                        break;
                    }

                    if (Arena != Rectangle.Empty && position.Y > Arena.Bottom)
                        _slam = SlamState.None;
                    break;

                case SlamState.Stuck:
                    _slamStateTimer -= dt;

                    if (_slamStateTimer <= 0)
                        _slam = SlamState.None;
                    break;
            }
        }

        // =====================================================
        // DRAW
        // =====================================================

        // boss.png: row 0 hover, row 1 thrust (rise / dive / stuck), row 2 red eye (firing, laser)
        private Rectangle CurrentSource(Texture2D texture)
        {
            switch (_slam)
            {
                case SlamState.Rise:
                    return SheetFrame(texture, 1, 0);

                case SlamState.Dive:
                    return SheetFrame(texture, 1, 1 + (int)(_time * 14f) % 2);

                case SlamState.Stuck:
                    return SheetFrame(texture, 1, 3);
            }

            switch (_beam)
            {
                case BeamState.Telegraph:
                    return SheetFrame(texture, 2, (int)(_time * 10f) % 2);

                case BeamState.Sweep:
                    return SheetFrame(texture, 2, 2);
            }

            if (_sinceShot < 0.35f)
                return SheetFrame(texture, 2, _sinceShot < 0.12f ? 1 : 3);

            if (_attackElapsed < 0.5f)
                return SheetFrame(texture, 2, 2);   // summoning drones

            return SheetFrame(texture, 0, LoopFrame(texture, 6f));
        }

        public override SpriteShape GetShape()
        {
            Texture2D texture = TextureManager.Boss;

            if (texture == null)
                return SpriteShape.FromRect(Bounds);

            return SpriteShape.FromTexture(texture, CurrentSource(texture), Bounds);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead || Despawned)
                return;

            DrawBeam(spriteBatch);

            Rectangle b = Bounds;
            Texture2D texture = TextureManager.Boss;

            Color phaseColor =
                Phase == 1 ? new Color(30, 30, 40) :
                Phase == 2 ? new Color(70, 20, 30) :
                new Color(90, 20, 90);

            if (texture != null)
            {
                Color tint =
                    _damagedFlash > 0 ? Color.Red :
                    _slam == SlamState.Stuck ? Color.LightBlue :
                    Phase == 3 ? new Color(255, 205, 225) :
                    Phase == 2 ? new Color(255, 225, 225) :
                    Color.White;

                Rectangle source = CurrentSource(texture);

                DrawHighlight(spriteBatch, texture, b, source, SpriteEffects.None);
                spriteBatch.Draw(texture, b, source, tint);
                return;
            }

            // rotating ring
            float ring = (float)(_time * 2.0);
            for (int i = 0; i < 6; i++)
            {
                float a = ring + i * MathHelper.TwoPi / 6f;
                Vector2 p = Center + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 62f;
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle((int)p.X - 5, (int)p.Y - 5, 10, 10), Color.Red * 0.7f);
            }

            spriteBatch.Draw(TextureManager.Pixel, b, FlashOr(_slam == SlamState.Stuck ? new Color(60, 80, 140) : phaseColor));

            // the eye
            float pulse = 0.7f + 0.3f * (float)Math.Sin(_time * 5f);
            Rectangle eye = new Rectangle(b.Center.X - 14, b.Center.Y - 14, 28, 28);
            spriteBatch.Draw(TextureManager.Pixel, eye, Color.Red * pulse);
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(eye.Center.X - 5, eye.Center.Y - 5, 10, 10), Color.White);
        }

        private void DrawBeam(SpriteBatch spriteBatch)
        {
            if (_beam == BeamState.Off)
                return;

            bool telegraph = _beam == BeamState.Telegraph;

            float thickness = telegraph ? 2f : 14f;
            Color color = telegraph
                ? Color.Red * (0.4f + 0.4f * (float)Math.Sin(_time * 30f))
                : Color.Lerp(Color.Red, Color.White, 0.35f);

            spriteBatch.Draw(
                TextureManager.Pixel,
                Center,
                null,
                color,
                _beamAngle,
                new Vector2(0, 0.5f),
                new Vector2(_beamLength, thickness),
                SpriteEffects.None,
                0f);

            if (!telegraph)
            {
                spriteBatch.Draw(
                    TextureManager.Pixel,
                    Center,
                    null,
                    Color.White * 0.8f,
                    _beamAngle,
                    new Vector2(0, 0.5f),
                    new Vector2(_beamLength, 4f),
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}
