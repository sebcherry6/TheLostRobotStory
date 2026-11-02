using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    // Shared base for every boss: activation, phases, hit cooldown,
    // arena bounds and summoned enemies.
    public abstract class Boss : Enemy
    {
        // Screen shake hook (GameplayScene listens)
        public static Action BossSlamEvent;

        public string Name { get; protected set; } = "BOSS";

        // Used for story event ids: "<StoryId>_intro", "<StoryId>_defeated"
        public string StoryId { get; protected set; } = "boss";

        // Extra line under the boss bar (e.g. "ADAPTED: MELEE RESISTANT")
        public virtual string Status => "";

        public bool Activated { get; protected set; }

        // The room the boss fights in (set by Level). Empty = no limit.
        public Rectangle Arena;

        // Enemies the boss wants to add to the level (drained by GameManager)
        public readonly List<Enemy> SpawnedEnemies = new();

        protected float ActivationRange = 520f;

        // animation helpers (time since the last shot / landing, current attack)
        protected float _sinceShot = 10f;
        protected float _sinceLanding = 10f;
        protected float _attackElapsed = 10f;
        protected bool _moving;
        protected float _bossHitCooldown;
        protected float _damagedFlash;

        protected Boss(Vector2 startPos, int maxHealth, Vector2 bossSize)
            : base(startPos, EnemyType.Normal)
        {
            size = bossSize;
            MaxHealth = maxHealth;
            Health = maxHealth;
            ContactDamage = 1;
        }

        public int Phase
        {
            get
            {
                float hp = (float)Health / MaxHealth;

                if (hp > 0.66f) return 1;
                if (hp > 0.33f) return 2;
                return 3;
            }
        }

        public void ForceActivate() => Activated = true;

        public override bool TakeHit(int damage, bool ranged, int fromSide)
        {
            if (!Activated)
                Activated = true;

            TakeDamage(damage);
            return true;
        }

        public override void TakeDamage(int damage)
        {
            if (_bossHitCooldown > 0)
                return;

            Health = Math.Max(0, Health - damage);

            _bossHitCooldown = 0.2f;
            _damagedFlash = 0.1f;

            AudioManager.Play("boss_hit");
        }

        public override void Update(GameTime gameTime, List<Rectangle> solids, Player player, List<Projectile> projectiles)
        {
            if (IsDead || Despawned)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            if (!Activated)
            {
                bool inArena = Arena != Rectangle.Empty && Arena.Contains(player.Center);
                bool inRange = Vector2.Distance(Center, player.Center) <= ActivationRange;

                if (inArena || inRange)
                    Activated = true;
                else
                {
                    // stand still until the fight begins
                    if (!Flying)
                        FallAndCollide(solids, dt);

                    return;
                }
            }

            if (_bossHitCooldown > 0) _bossHitCooldown -= dt;
            if (_damagedFlash > 0) _damagedFlash -= dt;

            _sinceShot += dt;
            _sinceLanding += dt;
            _attackElapsed += dt;

            UpdateBoss(solids, player, projectiles, dt);
        }

        protected virtual bool Flying => false;

        protected abstract void UpdateBoss(List<Rectangle> solids, Player player, List<Projectile> projectiles, float dt);

        // ---------- helpers ----------

        protected void FireAt(Player player, List<Projectile> projectiles, float speed, ProjectileKind kind = ProjectileKind.Bolt, float spread = 0f)
        {
            Vector2 direction = player.Center - Center;

            if (direction == Vector2.Zero)
                direction = new Vector2(_direction, 0);

            direction.Normalize();

            if (spread != 0f)
            {
                float angle = (float)Math.Atan2(direction.Y, direction.X) + spread;
                direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            }

            projectiles.Add(new Projectile(Center + direction * (size.X / 2f), direction, false, speed, kind));
        }

        protected void SlamShockwaves(List<Projectile> projectiles, float speed)
        {
            projectiles.Add(Projectile.Shockwave(Bounds.Left - 8, Bounds.Bottom, -1, speed));
            projectiles.Add(Projectile.Shockwave(Bounds.Right + 8, Bounds.Bottom, 1, speed));

            BossSlamEvent?.Invoke();
        }

        protected void ClampToArena()
        {
            if (Arena == Rectangle.Empty)
                return;

            position.X = MathHelper.Clamp(position.X, Arena.Left + 8, Arena.Right - size.X - 8);
            position.Y = MathHelper.Clamp(position.Y, Arena.Top + 8, Arena.Bottom - size.Y);
        }

        // red pulsing outline that follows the sprite, so bosses always stand out
        protected void DrawHighlight(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, Rectangle source, SpriteEffects effects)
        {
            float pulse = 0.55f + 0.35f * (float)Math.Sin(_time * 5f);
            Color color = Activated ? new Color(255, 60, 60) * pulse : Color.White * 0.25f;

            Sprites.DrawOutline(spriteBatch, texture, destination, source, color, effects, 2);
        }

        // =====================================================
        // BOSS SHEETS: frames of size x size, one row per animation
        // (row 0 = move, row 1 = jump / dash / thrust, row 2 = attack).
        // A sheet with fewer rows falls back to row 0.
        // =====================================================

        protected Rectangle SheetFrame(Texture2D texture, int row, int frame)
        {
            int fw = (int)size.X;
            int fh = (int)size.Y;
            int rows = Math.Max(1, texture.Height / fh);
            int count = Math.Max(1, texture.Width / fw);

            if (row >= rows)
                row = 0;

            frame = Math.Clamp(frame, 0, count - 1);
            return new Rectangle(frame * fw, row * fh, fw, fh);
        }

        protected int LoopFrame(Texture2D texture, float fps) =>
            Sprites.AnimFrame(texture, (int)size.X, fps);

        // airborne frame: 0 fast rising ... 3 falling
        protected int AirFrame() =>
            velocity.Y < -250f ? 0 : velocity.Y < 0f ? 1 : velocity.Y < 300f ? 2 : 3;

        protected Color FlashOr(Color color) => _damagedFlash > 0 ? Color.White : color;

        protected void DrawEye(SpriteBatch spriteBatch, Color color, int eyeSize = 10)
        {
            int x = _direction > 0 ? Bounds.Right - eyeSize - 8 : Bounds.X + 8;
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle(x, Bounds.Y + 14, eyeSize, eyeSize / 2 + 2), color);
        }
    }
}
