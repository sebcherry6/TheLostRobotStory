using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    // Normal  = Security Unit     (patrols, chases, swings its claw at close range)
    // Fast    = Interceptor       (chases, jumps gaps, pounces at you)
    // Tank    = Heavy Enforcer    (front shield, charges, vulnerable after a charge)
    // Laser   = Sentry Unit       (mobile shooter)
    // Turret  = Automated turret  (stationary, aims at you through open space)
    // Relay   = Power relay       (objective, does not fight back)
    // Drone   = Security drone    (flying, summoned by THE CORE)
    public enum EnemyType { Normal, Fast, Tank, Laser, Turret, Relay, Drone }

    public class Enemy : Entity
    {
        // Solid lookup for line-of-sight checks (set by Level when it loads)
        public static Func<Vector2, bool> SolidQuery;

        public EnemyType Type;

        public int Health = 1;
        public int MaxHealth = 1;
        public bool IsDead => Health <= 0;

        // Removed without being destroyed (e.g. WARDEN-7 retreating)
        public bool Despawned;

        public int ContactDamage = 1;

        // Optional id used for saving (relays) or grouping (ambush waves)
        public string Tag;

        public virtual bool CountsAsEnemy =>
            Type != EnemyType.Relay && !(this is Boss);

        protected float _hitCooldown;
        protected float _flashTimer;

        // MOVEMENT
        protected float _speed;
        protected int _direction = 1;
        protected bool _grounded;
        protected const float Gravity = 1600f;
        protected const float MaxFallSpeed = 800f;
        private float _jumpCooldown;

        // SHOOTING
        protected float _shootTimer = 2f;
        protected float _shootCooldown = 2f;
        private float _sightTimer;
        private bool _canSeePlayer;
        private Vector2 _aim = new Vector2(1, 0);

        // TANK
        private enum TankState { Patrol, Windup, Charge, Stunned }
        private TankState _tankState = TankState.Patrol;
        private float _tankTimer;
        private float _tankCooldown;

        // MELEE / POUNCE (security units and interceptors)
        private enum AttackState { None, Windup, Strike, Recover }
        private AttackState _attack = AttackState.None;
        private float _attackTimer;
        private float _attackCooldown = 1f;
        private float _attackDuration = 1f;
        private bool _strikeLanded;
        private float _leapSpeed;

        protected float _time;

        // constant per-enemy offset so neighbours don't animate in sync
        protected float _animOffset;

        // time since the last shot (drives fire / recoil frames)
        private float _sinceShot = 10f;

        // animation tuning
        private const float WalkFps = 6f;

        public Enemy(Vector2 startPos, EnemyType type)
        {
            position = startPos;
            size = new Vector2(32, 32);
            Type = type;

            switch (Type)
            {
                case EnemyType.Normal:
                    _speed = 62f;
                    Health = 2;
                    break;

                case EnemyType.Fast:
                    _speed = 88f;
                    Health = 1;
                    size = new Vector2(28, 24);
                    position += new Vector2(2, 8);
                    break;

                case EnemyType.Tank:
                    _speed = 34f;
                    Health = 6;
                    ContactDamage = 2;
                    size = new Vector2(40, 40);
                    position += new Vector2(-4, -8);
                    break;

                case EnemyType.Laser:
                    _speed = 48f;
                    Health = 3;
                    _shootCooldown = 2.4f;
                    break;

                case EnemyType.Turret:
                    _speed = 0f;
                    Health = 3;
                    _shootCooldown = 1.8f;
                    _shootTimer = 1.8f;
                    size = new Vector2(28, 28);
                    position += new Vector2(2, 0);    // mount plate touches the ceiling
                    _aim = new Vector2(0, 1);         // hangs pointing down until it sees you
                    break;

                case EnemyType.Relay:
                    _speed = 0f;
                    Health = 8;
                    ContactDamage = 0;
                    size = new Vector2(32, 64);
                    break;

                case EnemyType.Drone:
                    _speed = 90f;
                    Health = 1;
                    size = new Vector2(22, 22);
                    break;
            }

            MaxHealth = Health;
            _animOffset = startPos.X * 0.013f + startPos.Y * 0.007f;
        }

        // =====================================================
        // DAMAGE
        // ranged: true for projectiles, false for melee
        // fromSide: side of THIS enemy the hit arrives at (-1 left, 1 right)
        // Returns false when the hit was blocked.
        // =====================================================

        public virtual bool TakeHit(int damage, bool ranged, int fromSide)
        {
            if (Type == EnemyType.Tank &&
                _tankState != TankState.Stunned &&
                fromSide == _direction)
            {
                AudioManager.Play("block");
                _flashTimer = 0.05f;
                return false;
            }

            int finalDamage = Type == EnemyType.Tank && _tankState == TankState.Stunned
                ? damage * 2
                : damage;

            TakeDamage(finalDamage);
            return true;
        }

        public virtual void TakeDamage(int damage)
        {
            if (_hitCooldown > 0)
                return;

            Health -= damage;

            AudioManager.Play(Type == EnemyType.Relay ? "boss_hit" : "enemy_hit");

            _hitCooldown = 0.15f;
            _flashTimer = 0.15f;

            // getting hit interrupts a wind-up
            if (_attack == AttackState.Windup)
                EndAttack(0.4f);
        }

        // =====================================================
        // UPDATE
        // =====================================================

        public virtual void Update(GameTime gameTime, List<Rectangle> solids, Player player, List<Projectile> projectiles)
        {
            if (IsDead || Despawned)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            if (_hitCooldown > 0) _hitCooldown -= dt;
            if (_flashTimer > 0) _flashTimer -= dt;
            if (_jumpCooldown > 0) _jumpCooldown -= dt;
            if (_attackCooldown > 0) _attackCooldown -= dt;
            _sinceShot += dt;

            switch (Type)
            {
                case EnemyType.Normal:
                    UpdateSecurityUnit(solids, player, dt);
                    break;

                case EnemyType.Fast:
                    UpdateInterceptor(solids, player, dt);
                    break;

                case EnemyType.Tank:
                    UpdateEnforcer(solids, player, dt);
                    break;

                case EnemyType.Laser:
                    Walk(solids, dt, _speed, false);
                    UpdateShooting(player, projectiles, dt, 450f, 240f);
                    break;

                case EnemyType.Turret:
                    UpdateShooting(player, projectiles, dt, 420f, 220f);
                    break;

                case EnemyType.Drone:
                    UpdateDrone(player, dt);
                    break;

                case EnemyType.Relay:
                    break;
            }
        }

        // ----- Security Unit: patrol, chase, then wind up and swing its claw
        private void UpdateSecurityUnit(List<Rectangle> solids, Player player, float dt)
        {
            float dx = player.Center.X - Center.X;
            float dy = Math.Abs(player.Center.Y - Center.Y);

            if (_attack != AttackState.None)
            {
                UpdateClaw(solids, player, dt);
                return;
            }

            if (Math.Abs(dx) < 220 && dy < 48)
            {
                _direction = Math.Sign(dx) == 0 ? _direction : Math.Sign(dx);

                if (Math.Abs(dx) < 56 && _grounded && _attackCooldown <= 0)
                {
                    StartAttack(0.45f);
                    return;
                }

                Walk(solids, dt, _speed * 1.5f, false);
            }
            else
            {
                Walk(solids, dt, _speed, false);
            }
        }

        private void UpdateClaw(List<Rectangle> solids, Player player, float dt)
        {
            _attackTimer -= dt;

            switch (_attack)
            {
                case AttackState.Windup:
                    FallAndCollide(solids, dt);

                    if (_attackTimer <= 0)
                    {
                        _attack = AttackState.Strike;
                        _attackTimer = _attackDuration = 0.18f;
                        _strikeLanded = false;
                        AudioManager.Play("attack", 0.5f, 0.2f);
                    }
                    break;

                case AttackState.Strike:
                    // short lunge forward with the claw out
                    if (!_grounded || HasGroundAhead(solids))
                        MoveHorizontal(solids, _direction * 220f * dt);

                    FallAndCollide(solids, dt);

                    Rectangle claw = new Rectangle(
                        _direction > 0 ? Bounds.Right - 4 : Bounds.Left - 20,
                        Bounds.Y + 6, 24, Bounds.Height - 10);

                    if (!_strikeLanded && PixelCollision.Overlaps(player.GetShape(), claw))
                    {
                        _strikeLanded = true;
                        player.TakeDamage(1);
                    }

                    if (_attackTimer <= 0)
                        EndAttack(0.5f);
                    break;

                case AttackState.Recover:
                    FallAndCollide(solids, dt);

                    if (_attackTimer <= 0)
                    {
                        _attack = AttackState.None;
                        _attackCooldown = 1.1f;
                    }
                    break;
            }
        }

        // ----- Interceptor: chase, jump gaps, crouch and pounce
        private void UpdateInterceptor(List<Rectangle> solids, Player player, float dt)
        {
            float dx = player.Center.X - Center.X;
            float dy = player.Center.Y - Center.Y;

            if (_attack != AttackState.None)
            {
                UpdatePounce(solids, dt);
                return;
            }

            bool chasing = Math.Abs(dx) < 340 && Math.Abs(dy) < 180;

            if (chasing)
            {
                if (Math.Abs(dx) > 8)
                    _direction = Math.Sign(dx);

                if (_grounded && _attackCooldown <= 0 && Math.Abs(dx) < 170 && Math.Abs(dy) < 80)
                {
                    StartAttack(0.3f);
                    return;
                }

                // hop up to a player standing above
                if (_grounded && dy < -64 && Math.Abs(dx) < 120 && _jumpCooldown <= 0)
                    Jump();

                Walk(solids, dt, 150f, true);
            }
            else
            {
                Walk(solids, dt, _speed, false);
            }
        }

        private void UpdatePounce(List<Rectangle> solids, float dt)
        {
            _attackTimer -= dt;

            switch (_attack)
            {
                case AttackState.Windup:
                    FallAndCollide(solids, dt);

                    if (_attackTimer <= 0)
                    {
                        _attack = AttackState.Strike;
                        _attackTimer = _attackDuration = 1.2f;
                        _leapSpeed = _direction * 280f;
                        Jump(520f);
                        AudioManager.Play("jump", 0.5f, 0.3f);
                    }
                    break;

                case AttackState.Strike:
                    MoveHorizontal(solids, _leapSpeed * dt);
                    FallAndCollide(solids, dt);

                    if ((_grounded && velocity.Y >= 0 && _attackTimer < 1.1f) || _attackTimer <= 0)
                        EndAttack(0.45f);
                    break;

                case AttackState.Recover:
                    FallAndCollide(solids, dt);

                    if (_attackTimer <= 0)
                    {
                        _attack = AttackState.None;
                        _attackCooldown = 1.6f;
                    }
                    break;
            }
        }

        private void StartAttack(float windup)
        {
            _attack = AttackState.Windup;
            _attackTimer = _attackDuration = windup;
        }

        private void EndAttack(float recover)
        {
            _attack = AttackState.Recover;
            _attackTimer = _attackDuration = recover;
        }

        // ----- Heavy Enforcer: shielded front, wind-up -> charge -> stunned (open to attack)
        private void UpdateEnforcer(List<Rectangle> solids, Player player, float dt)
        {
            if (_tankCooldown > 0)
                _tankCooldown -= dt;

            float dx = player.Center.X - Center.X;
            float dy = Math.Abs(player.Center.Y - Center.Y);

            switch (_tankState)
            {
                case TankState.Patrol:
                    Walk(solids, dt, _speed, false);

                    if (_tankCooldown <= 0 && Math.Abs(dx) < 260 && dy < 64)
                    {
                        _direction = Math.Sign(dx) == 0 ? _direction : Math.Sign(dx);
                        _tankState = TankState.Windup;
                        _tankTimer = 0.7f;
                    }
                    break;

                case TankState.Windup:
                    FallAndCollide(solids, dt);
                    _tankTimer -= dt;

                    if (_tankTimer <= 0)
                    {
                        _tankState = TankState.Charge;
                        _tankTimer = 1.0f;
                        AudioManager.Play("boss_dash", 0.6f);
                    }
                    break;

                case TankState.Charge:
                    _tankTimer -= dt;

                    bool blocked =
                        (_grounded && !HasGroundAhead(solids)) ||
                        MoveHorizontal(solids, _direction * 230f * dt);

                    FallAndCollide(solids, dt);

                    if (blocked || _tankTimer <= 0)
                    {
                        _tankState = TankState.Stunned;
                        _tankTimer = 1.5f;

                        if (blocked)
                            AudioManager.Play("crusher", 0.5f);
                    }
                    break;

                case TankState.Stunned:
                    FallAndCollide(solids, dt);
                    _tankTimer -= dt;

                    if (_tankTimer <= 0)
                    {
                        _tankState = TankState.Patrol;
                        _tankCooldown = 2.2f;
                    }
                    break;
            }
        }

        public bool IsCharging => Type == EnemyType.Tank && _tankState == TankState.Charge;

        // ----- Drone: flies toward the player, ignores walls
        private void UpdateDrone(Player player, float dt)
        {
            Vector2 toPlayer = player.Center - Center;

            if (toPlayer.LengthSquared() < 700 * 700 && toPlayer != Vector2.Zero)
            {
                toPlayer.Normalize();
                velocity = Vector2.Lerp(velocity, toPlayer * _speed, 2.5f * dt);
            }

            position += velocity * dt;
            position.Y += (float)Math.Sin(_time * 5f) * 16f * dt;
            _direction = velocity.X < 0 ? -1 : 1;
        }

        // ----- Shooting with line of sight (sentries and turrets)
        private void UpdateShooting(Player player, List<Projectile> projectiles, float dt, float range, float bulletSpeed)
        {
            Vector2 toPlayer = player.Center - Center;
            float distance = toPlayer.Length();

            if (distance > range)
            {
                _canSeePlayer = false;
                return;
            }

            _sightTimer -= dt;

            if (_sightTimer <= 0)
            {
                _sightTimer = 0.2f;
                _canSeePlayer = HasLineOfSight(Center, player.Center);
            }

            if (!_canSeePlayer)
                return;

            if (toPlayer != Vector2.Zero)
                _aim = Vector2.Normalize(toPlayer);

            if (Type == EnemyType.Laser)
                _direction = _aim.X < 0 ? -1 : 1;

            _shootTimer -= dt;

            if (_shootTimer > 0)
                return;

            _shootTimer = _shootCooldown;

            projectiles.Add(new Projectile(Center + _aim * 18f, _aim, false, bulletSpeed));
            _sinceShot = 0f;
            AudioManager.Play("shoot", 0.4f, 0.2f);
        }

        private bool IsAboutToFire => _canSeePlayer && _shootTimer < 0.3f;

        public static bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            if (SolidQuery == null)
                return true;

            float distance = Vector2.Distance(from, to);
            int steps = Math.Max(1, (int)(distance / 16f));

            for (int i = 1; i < steps; i++)
            {
                if (SolidQuery(Vector2.Lerp(from, to, i / (float)steps)))
                    return false;
            }

            return true;
        }

        // =====================================================
        // MOVEMENT HELPERS (also used by bosses)
        // =====================================================

        protected void Walk(List<Rectangle> solids, float dt, float speed, bool canJump)
        {
            if (_grounded && !HasGroundAhead(solids))
            {
                if (canJump && _jumpCooldown <= 0)
                    Jump();
                else
                    _direction *= -1;
            }

            if (MoveHorizontal(solids, _direction * speed * dt))
            {
                if (canJump && _grounded && _jumpCooldown <= 0)
                    Jump();
                else
                    _direction *= -1;
            }

            FallAndCollide(solids, dt);
        }

        protected void Jump(float force = 600f)
        {
            velocity.Y = -force;
            _grounded = false;
            _jumpCooldown = 1.1f;
        }

        // returns true when a wall was hit
        protected bool MoveHorizontal(List<Rectangle> solids, float amount)
        {
            position.X += amount;

            foreach (Rectangle tile in solids)
            {
                if (!Bounds.Intersects(tile))
                    continue;

                position.X = amount > 0 ? tile.Left - size.X : tile.Right;
                return true;
            }

            return false;
        }

        protected void FallAndCollide(List<Rectangle> solids, float dt)
        {
            velocity.Y = Math.Min(velocity.Y + Gravity * dt, MaxFallSpeed);
            position.Y += velocity.Y * dt;

            _grounded = false;

            foreach (Rectangle tile in solids)
            {
                if (!Bounds.Intersects(tile))
                    continue;

                if (velocity.Y > 0)
                {
                    position.Y = tile.Top - size.Y;
                    _grounded = true;
                }
                else
                {
                    position.Y = tile.Bottom;
                }

                velocity.Y = 0;
            }
        }

        protected bool HasGroundAhead(List<Rectangle> solids)
        {
            Point probe = new Point(
                _direction > 0 ? Bounds.Right + 1 : Bounds.Left - 1,
                Bounds.Bottom + 4);

            foreach (Rectangle tile in solids)
            {
                if (tile.Contains(probe))
                    return true;
            }

            return false;
        }

        // =====================================================
        // ANIMATION
        // Enemy sheets: one row per animation, frames left to right.
        //   row 0 = walk   row 1 = jump / airborne   row 2 = attack
        // A sheet with only one row still works: missing rows fall back to
        // row 0 plus code-driven poses (wind-up shake, recoil, lunge).
        // =====================================================

        private enum Anim { Walk, Jump, Attack }

        private Anim CurrentAnim
        {
            get
            {
                if (Type == EnemyType.Turret || Type == EnemyType.Relay || Type == EnemyType.Drone)
                    return Anim.Walk;

                if (_attack != AttackState.None && _attack != AttackState.Recover)
                    return _attack == AttackState.Strike && Type == EnemyType.Fast ? Anim.Jump : Anim.Attack;

                if (Type == EnemyType.Tank && (_tankState == TankState.Windup || _tankState == TankState.Charge))
                    return Anim.Attack;

                if (Type == EnemyType.Laser && _sinceShot < 0.3f)
                    return Anim.Attack;

                if (!_grounded && Math.Abs(velocity.Y) > 60f)
                    return Anim.Jump;

                return Anim.Walk;
            }
        }

        private Rectangle CurrentSource(Texture2D texture)
        {
            int fw = (int)size.X;
            int fh = (int)size.Y;
            int rows = Math.Max(1, texture.Height / fh);
            int count = Math.Max(1, texture.Width / fw);

            if (Type == EnemyType.Drone || Type == EnemyType.Relay)
                count = Math.Min(count, 4);  // extra "destroyed" frames at the end

            Anim anim = CurrentAnim;
            int row = 0;
            int frame;

            switch (anim)
            {
                case Anim.Jump:
                    if (rows > 1)
                    {
                        row = 1;
                        // first half of the row while rising, second half while falling
                        frame = velocity.Y < 0 ? 0 : count - 1;
                        if (count > 2) frame = velocity.Y < -150 ? 0 : velocity.Y < 150 ? count / 2 : count - 1;
                    }
                    else
                    {
                        frame = 1 % count;   // hold a stride frame in the air
                    }
                    break;

                case Anim.Attack:
                    if (rows > 2)
                    {
                        row = 2;
                        float progress = 1f - MathHelper.Clamp(_attackTimer / Math.Max(0.01f, _attackDuration), 0f, 1f);

                        if (Type == EnemyType.Tank)
                            progress = _tankState == TankState.Charge ? 1f : 0.5f;
                        else if (Type == EnemyType.Laser)
                            progress = _sinceShot / 0.3f;

                        frame = Math.Clamp((int)(progress * count), 0, count - 1);
                    }
                    else
                    {
                        frame = Type == EnemyType.Tank && _tankState == TankState.Charge
                            ? Sprites.AnimFrame(texture, fw, WalkFps * 2.5f, _animOffset, count)
                            : 0;
                    }
                    break;

                default:
                    bool moving = Type == EnemyType.Drone || Type == EnemyType.Relay ||
                                  (Type == EnemyType.Tank && _tankState == TankState.Patrol) ||
                                  (Type != EnemyType.Tank && _attack == AttackState.None);

                    frame = moving ? Sprites.AnimFrame(texture, fw, WalkFps, _animOffset, count) : 0;
                    break;
            }

            row = Math.Min(row, rows - 1);
            return new Rectangle(frame * fw, row * fh, fw, fh);
        }

        // code-driven pose offset when the sheet has no attack row (whole pixels only)
        private Point PoseOffset(Texture2D texture)
        {
            int rows = Math.Max(1, texture.Height / (int)size.Y);

            // wind-up: shudder in place
            bool windingUp =
                _attack == AttackState.Windup ||
                (Type == EnemyType.Tank && _tankState == TankState.Windup);

            if (windingUp && rows <= 2)
                return new Point(((int)(_time * 30) % 2 == 0 ? -1 : 1), 0);

            // recoil after a shot
            if (Type == EnemyType.Laser && _sinceShot < 0.12f)
                return new Point(-_direction * 2, 0);

            return Point.Zero;
        }

        private bool ShowWindupFlash =>
            (_attack == AttackState.Windup || (Type == EnemyType.Tank && _tankState == TankState.Windup)) &&
            (int)(_time * 12) % 2 == 0;

        // =====================================================
        // DRAW
        // =====================================================

        private Texture2D TextureForType()
        {
            Texture2D specific = Type switch
            {
                EnemyType.Fast => TextureManager.EnemyFast,
                EnemyType.Tank => TextureManager.EnemyTank,
                EnemyType.Laser => TextureManager.EnemyLaser,
                EnemyType.Turret => TextureManager.Get("turret"),
                EnemyType.Relay => TextureManager.Get("relay"),
                EnemyType.Drone => TextureManager.Get("drone"),
                _ => TextureManager.EnemyNormal
            };

            if (Type == EnemyType.Turret || Type == EnemyType.Relay || Type == EnemyType.Drone)
                return specific;

            return specific ?? TextureManager.Enemy;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead || Despawned)
                return;

            Texture2D texture = TextureForType();
            SpriteEffects flip = _direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            if (texture != null && Type == EnemyType.Turret)
            {
                DrawTurret(spriteBatch, texture);
            }
            else if (texture != null)
            {
                Color tint =
                    _flashTimer > 0 ? Color.Red :
                    ShowWindupFlash ? new Color(255, 210, 140) :
                    Color.White;

                if (Type == EnemyType.Tank && _tankState == TankState.Stunned)
                    tint = Color.LightBlue;

                Rectangle source = CurrentSource(texture);
                Point offset = PoseOffset(texture);

                // interceptors lean into their leap (sheet without a jump row)
                float rotation = 0f;

                if (Type == EnemyType.Fast && !_grounded && texture.Height / (int)size.Y < 2)
                    rotation = MathHelper.Clamp(velocity.Y / 1500f, -0.35f, 0.35f) * _direction;

                Vector2 centre = new Vector2(
                    (int)Math.Round(position.X + size.X / 2f) + offset.X,
                    (int)Math.Round(position.Y + size.Y / 2f) + offset.Y);

                spriteBatch.Draw(texture, centre, source, tint, rotation, new Vector2(source.Width / 2f, source.Height / 2f), 1f, flip, 0f);

                DrawMuzzleFlash(spriteBatch);
            }
            else
            {
                DrawPlaceholder(spriteBatch);
            }

            // small health bar for tougher enemies once damaged
            if (MaxHealth > 2 && Health < MaxHealth && !(this is Boss))
            {
                Rectangle back = new Rectangle(Bounds.X, Bounds.Y - 8, Bounds.Width, 4);
                spriteBatch.Draw(TextureManager.Pixel, back, Color.Black * 0.6f);
                spriteBatch.Draw(TextureManager.Pixel, new Rectangle(back.X, back.Y, back.Width * Health / MaxHealth, 4), Color.Red);
            }
        }

        private void DrawMuzzleFlash(SpriteBatch spriteBatch)
        {
            if (Type != EnemyType.Laser || _sinceShot > 0.08f)
                return;

            Vector2 tip = Center + _aim * 20f;
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle((int)tip.X - 3, (int)tip.Y - 3, 6, 6), Color.White);
            spriteBatch.Draw(TextureManager.Pixel, new Rectangle((int)tip.X - 5, (int)tip.Y - 1, 10, 2), Color.Orange);
        }

        // =====================================================
        // TURRET (turret.png = 5 frames of 28x28)
        //   0: ceiling mount (static)
        //   1: idle  2: alert  3: fire  4: recoil  (gun head, barrel points right)
        // =====================================================

        private int TurretHeadFrame =>
            _sinceShot < 0.08f ? 3 :
            _sinceShot < 0.22f ? 4 :
            _canSeePlayer ? 2 : 1;

        private void DrawTurret(SpriteBatch spriteBatch, Texture2D texture)
        {
            Rectangle b = Bounds;
            Color tint = _flashTimer > 0 ? Color.Red : Color.White;

            Sprites.DrawFrame(spriteBatch, texture, b, 0, tint, 28);

            float angle = (float)Math.Atan2(_aim.Y, _aim.X);

            spriteBatch.Draw(
                texture,
                new Vector2(b.X + 14, b.Y + 14),
                Sprites.FrameRect(texture, TurretHeadFrame, 28),
                tint,
                angle,
                new Vector2(14, 14),
                1f,
                SpriteEffects.None,
                0f);
        }

        // =====================================================
        // PIXEL SHAPE (for pixel-perfect hits)
        // =====================================================

        public override SpriteShape GetShape()
        {
            Texture2D texture = TextureForType();

            if (texture == null)
                return SpriteShape.FromRect(Bounds);

            if (Type == EnemyType.Turret)
                return SpriteShape.FromTexture(texture, Sprites.FrameRect(texture, TurretHeadFrame, 28), Bounds);

            return SpriteShape.FromTexture(texture, CurrentSource(texture), Bounds, _direction < 0);
        }

        private void DrawPlaceholder(SpriteBatch spriteBatch)
        {
            Rectangle b = Bounds;
            bool flash = _flashTimer > 0;
            Texture2D px = TextureManager.Pixel;

            switch (Type)
            {
                case EnemyType.Normal:
                    spriteBatch.Draw(px, b, flash ? Color.White : new Color(170, 40, 40));
                    spriteBatch.Draw(px, new Rectangle(_direction > 0 ? b.Right - 14 : b.X + 2, b.Y + 8, 12, 5), Color.Yellow);
                    break;

                case EnemyType.Fast:
                    spriteBatch.Draw(px, b, flash ? Color.White : Color.Orange);
                    spriteBatch.Draw(px, new Rectangle(_direction > 0 ? b.Right - 8 : b.X + 2, b.Y + 6, 6, 4), Color.White);
                    break;

                case EnemyType.Tank:
                    Color body =
                        flash ? Color.White :
                        _tankState == TankState.Stunned ? new Color(90, 90, 160) :
                        _tankState == TankState.Windup && (int)(_time * 16) % 2 == 0 ? Color.Yellow :
                        new Color(110, 20, 20);

                    spriteBatch.Draw(px, b, body);

                    // front shield plate (hits here are blocked)
                    if (_tankState != TankState.Stunned)
                    {
                        Rectangle shield = new Rectangle(_direction > 0 ? b.Right - 6 : b.X, b.Y - 2, 6, b.Height + 2);
                        spriteBatch.Draw(px, shield, Color.LightGray);
                    }
                    else
                    {
                        // sparks while stunned
                        if ((int)(_time * 10) % 2 == 0)
                            spriteBatch.Draw(px, new Rectangle(b.Center.X - 2, b.Y - 6, 4, 4), Color.Cyan);
                    }
                    break;

                case EnemyType.Laser:
                    spriteBatch.Draw(px, b, flash ? Color.White : Color.Purple);
                    DrawBarrel(spriteBatch, 18, Color.Violet);
                    break;

                case EnemyType.Turret:
                    spriteBatch.Draw(px, b, flash ? Color.White : new Color(80, 85, 95));
                    spriteBatch.Draw(px, new Rectangle(b.Center.X - 4, b.Center.Y - 4, 8, 8), IsAboutToFire ? Color.White : Color.Red);
                    DrawBarrel(spriteBatch, 20, Color.Gray);
                    break;

                case EnemyType.Relay:
                    float pulse = 0.6f + 0.4f * (float)Math.Sin(_time * 4f);
                    spriteBatch.Draw(px, b, flash ? Color.White : new Color(40, 50, 70));
                    spriteBatch.Draw(px, new Rectangle(b.X + 8, b.Y + 8, b.Width - 16, b.Height - 16), Color.Cyan * pulse);
                    spriteBatch.Draw(px, new Rectangle(b.X - 4, b.Bottom - 6, b.Width + 8, 6), new Color(60, 60, 70));
                    break;

                case EnemyType.Drone:
                    spriteBatch.Draw(px, b, flash ? Color.White : new Color(200, 40, 60));
                    spriteBatch.Draw(px, new Rectangle(b.Center.X - 3, b.Center.Y - 3, 6, 6), (int)(_time * 8) % 2 == 0 ? Color.White : Color.Red);
                    break;
            }
        }

        private void DrawBarrel(SpriteBatch spriteBatch, float length, Color color)
        {
            float angle = (float)Math.Atan2(_aim.Y, _aim.X);

            spriteBatch.Draw(
                TextureManager.Pixel,
                Center,
                null,
                color,
                angle,
                new Vector2(0, 0.5f),
                new Vector2(length, 4),
                SpriteEffects.None,
                0f);
        }
    }
}
