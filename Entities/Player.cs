using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Systems;
using TheLostRobotStory.World;

namespace TheLostRobotStory.Entities
{
    public class Player : Entity
    {

        // =====================================================
        // EVOLUTION
        // =====================================================

        public int EvolutionStage { get; private set; }

        // horizontal push from conveyor belts (px/s)
        public float ConveyorPush;

        // movement feel
        private const float GroundAcceleration = 1500f;
        private const float AirAcceleration = 950f;
        private const float GroundDeceleration = 2200f;
        private const float FallGravityMultiplier = 1.35f;   // snappier landings
        private const float ApexGravityMultiplier = 0.6f;    // brief hang at the top of a jump
        private const float JumpCutMultiplier = 0.45f;       // release SPACE early = smaller jump
        private bool _jumpHeld;
        private bool _jumpCut;
        private float _airTime;

        // visible body inside the 32x32 sprite frame (measured from player.png)
        private static Rectangle? _spriteBody;

        // =====================================================
        // EVENTS (used by GameplayScene for audio / feedback)
        // =====================================================

        public event Action Hurt;
        public event Action Died;

        // True only on the frame a melee swing starts
        public bool AttackStarted { get; private set; }

        // =====================================================
        // DASH (unlocked at evolution stage 3)
        // =====================================================

        private float _dashTimer;
        private float _dashCooldownTimer;
        private const float DashDuration = 0.18f;
        private const float DashCooldown = 0.8f;
        private const float DashSpeed = 600f;

        public bool IsDashing => _dashTimer > 0;

        // =====================================================
        // PROCEDURAL ANIMATION (squash / stretch)
        // =====================================================

        private float _scaleX = 1f;
        private float _scaleY = 1f;
        private float _landSquash;
        private bool _wasGrounded;


        public bool HasShownShootMessage;

        public bool HasShownDoubleJumpMessage;



        // =====================================================
        // HEALTH
        // =====================================================

        public int MaxHealth = 3;

        public int Health = 3;




        // =====================================================
        // CHECKPOINT
        // =====================================================

        public Vector2 SpawnPoint;


        private int _checkpointEvolution;

        private int _checkpointMaxHealth;

        private int _checkpointHealth;




        // =====================================================
        // MOVEMENT
        // =====================================================

        public int FacingDirection = 1;


        private float _speed = 140f;

        private float _gravity = 1600f;

        private float _jumpForce = -800f;

        private float _maxFallSpeed = 700f;



        private bool _grounded;

        private bool _doubleJumpUsed;



        private bool _standingOnPlatform;





        // =====================================================
        // JUMP BUFFER / COYOTE TIME
        // =====================================================

        private float _coyoteTimer;

        private readonly float _coyoteTime = 0.15f;



        private float _jumpBufferTimer;

        private readonly float _jumpBufferTime = 0.15f;






        // =====================================================
        // ATTACK
        // =====================================================

        public bool IsAttacking;


        private float _attackTimer;

        private readonly float _attackCooldown = 0.3f;







        // =====================================================
        // DAMAGE
        // =====================================================

        private float _invincibleTimer;

        private readonly float _invincibleDuration = 1f;






        // =====================================================
        // ANIMATION
        // =====================================================

        private AnimationSystem _animation;







        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Player(
            Vector2 startPosition)
        {

            position =
                startPosition;



            SpawnPoint =
                startPosition;



            size =
                new Vector2(
                    32,
                    32);



            velocity =
                Vector2.Zero;



            ApplySpriteBody();

            _animation =
                new AnimationSystem();

            SetupAnimations();




            _checkpointEvolution =
                EvolutionStage;



            _checkpointMaxHealth =
                MaxHealth;



            _checkpointHealth =
                Health;

        }








        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(
            GameTime gameTime,
            InputManager input,
            List<Projectile> projectiles,
            List<Rectangle> solids,
            List<MovingPlatform> platforms)
        {

            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;




            _standingOnPlatform =
                false;




            HandleMovement(
                input,
                dt);

            HandleDash(
                input,
                dt);



            HandleJump(
                input,
                dt);

            HandleJumpCut(
                input);



            ApplyGravity(
                dt);



            HandleShooting(
                input,
                projectiles);



            HandleAttack(
                input,
                dt);




            UpdateAnimationState(
                dt);

            _animation.Update(
                gameTime);





            if (_invincibleTimer > 0)
            {

                _invincibleTimer -= dt;

            }


        }











        private void UpdateEvolutionSprite()
        {
            // Reserved for animated sprites later
        }








        // =====================================================
        // MOVEMENT
        // =====================================================

        private void HandleMovement(
            InputManager input,
            float dt)
        {
            float speed = GetSpeed();
            float target = 0f;

            if (input.Left())
            {
                target = -speed;
                FacingDirection = -1;
            }

            if (input.Right())
            {
                target = speed;
                FacingDirection = 1;
            }

            // accelerate toward the target speed instead of snapping to it
            float rate =
                target != 0f ? (_grounded ? GroundAcceleration : AirAcceleration) :
                (_grounded ? GroundDeceleration : AirAcceleration * 0.5f);

            if (velocity.X < target)
                velocity.X = Math.Min(target, velocity.X + rate * dt);
            else if (velocity.X > target)
                velocity.X = Math.Max(target, velocity.X - rate * dt);

            // position is applied in ApplyCollision
        }









        // =====================================================
        // SPEED BY EVOLUTION
        // =====================================================

        private float GetSpeed()
        {

            switch (EvolutionStage)
            {

                case 1:
                    return 146f;

                case 2:
                    return 152f;

                case 3:
                    return 158f;

                case 4:
                    return 170f;


                default:
                    return _speed;

            }

        }









        // =====================================================
        // JUMP SYSTEM
        // =====================================================

        private void HandleJump(
            InputManager input,
            float dt)
        {


            if (_grounded)
            {

                _coyoteTimer =
                    _coyoteTime;



                _doubleJumpUsed =
                    false;

            }
            else
            {

                _coyoteTimer -=
                    dt;

            }







            if (input.JumpPressed())
            {

                _jumpBufferTimer =
                    _jumpBufferTime;

            }
            else
            {

                _jumpBufferTimer -=
                    dt;

            }








            if (_jumpBufferTimer > 0)
            {


                // normal jump

                if (_coyoteTimer > 0)
                {

                    velocity.Y =
                        _jumpForce;



                    _grounded =
                        false;



                    _coyoteTimer =
                        0;

                    AudioManager.Play("jump");

                    _jumpCut = false;



                    _jumpBufferTimer =
                        0;

                }





                // double jump

                else if (EvolutionStage >= 1 &&
                        !_doubleJumpUsed)
                {

                    velocity.Y =
                        _jumpForce;



                    _doubleJumpUsed =
                        true;

                    _animation.Restart("jump");

                    AudioManager.Play("double_jump");

                    _jumpCut = false;



                    _jumpBufferTimer =
                        0;

                }

            }


        }








        // =====================================================
        // GRAVITY
        // =====================================================

        private new void ApplyGravity(float dt)
        {
            if (IsDashing)
            {
                velocity.Y = 0;
                return;
            }

            float multiplier = 1f;

            if (velocity.Y > 0)
                multiplier = FallGravityMultiplier;
            else if (_jumpHeld && Math.Abs(velocity.Y) < 90f)
                multiplier = ApexGravityMultiplier;

            velocity.Y += _gravity * multiplier * dt;

            if (velocity.Y > _maxFallSpeed)
                velocity.Y = _maxFallSpeed;
        }

        // variable jump height: letting go of jump while rising cuts the jump short
        private void HandleJumpCut(InputManager input)
        {
            _jumpHeld = input.JumpHeld();

            if (_grounded)
                _jumpCut = false;

            if (!_jumpHeld && !_jumpCut && velocity.Y < -120f && !IsDashing)
            {
                velocity.Y *= JumpCutMultiplier;
                _jumpCut = true;
            }
        }
        // =====================================================
        // SHOOTING
        // =====================================================

        private void HandleShooting(
            InputManager input,
            List<Projectile> projectiles)
        {

            if (EvolutionStage < 2)
                return;



            if (input.ShootPressed())
            {

                Shoot(
                    projectiles);

            }

        }








        private void Shoot(
            List<Projectile> projectiles)
        {
            AudioManager.Play("shoot");


            Vector2 spawn =
                position +
                new Vector2(
                    size.X / 2,
                    size.Y / 2);





            // =============================
            // EVOLUTION 1
            // =============================

            if (EvolutionStage == 2)
            {

                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            0),
                        true));

            }





            // =============================
            // EVOLUTION 2+
            // =============================

            if (EvolutionStage >= 3)
            {

                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            0),
                        true));



                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            -0.25f),
                        true));



                projectiles.Add(
                    new Projectile(
                        spawn,
                        new Vector2(
                            FacingDirection,
                            0.25f),
                        true));

            }

        }









        // =====================================================
        // ATTACK SYSTEM
        // =====================================================

        private void HandleAttack(
            InputManager input,
            float dt)
        {
            AttackStarted = false;


            if (_attackTimer > 0)
            {

                _attackTimer -=
                    dt;


                IsAttacking =
                    true;

            }
            else
            {

                IsAttacking =
                    false;

            }







            if (input.AttackPressed()
                &&
                _attackTimer <= 0)
            {

                _attackTimer =
                    _attackCooldown;

                AttackStarted = true;

                AudioManager.Play("attack");

            }

        }









        // =====================================================
        // ATTACK HITBOX
        // =====================================================

        public const int AttackReach = 54;

        public Rectangle AttackHitbox
        {
            get
            {
                if (!IsAttacking)
                    return Rectangle.Empty;

                int top = Bounds.Top - 6;
                int height = Bounds.Height + 10;

                return FacingDirection == 1
                    ? new Rectangle(Bounds.Right - 4, top, AttackReach, height)
                    : new Rectangle(Bounds.Left - AttackReach + 4, top, AttackReach, height);
            }
        }










        // =====================================================
        // SOLID COLLISION
        // =====================================================

        public void ApplyCollision(
    List<Rectangle> solids,
    float dt)
        {

            _grounded = false;



            // ==========================
            // HORIZONTAL
            // ==========================

            float moveX = velocity.X + ConveyorPush;

            position.X += moveX * dt;



            Rectangle bounds = Bounds;


            foreach (Rectangle tile in solids)
            {

                if (!bounds.Intersects(tile))
                    continue;



                if (moveX > 0)
                {
                    position.X =
                        tile.Left - size.X;
                }
                else if (moveX < 0)
                {
                    position.X =
                        tile.Right;
                }


                velocity.X = 0;


                bounds = Bounds;

            }






            // ==========================
            // VERTICAL
            // ==========================

            position.Y += velocity.Y * dt;



            bounds = Bounds;



            foreach (Rectangle tile in solids)
            {

                if (!bounds.Intersects(tile))
                    continue;



                if (velocity.Y > 0)
                {
                    // landing

                    position.Y =
                        tile.Top - size.Y;


                    _grounded = true;


                    _doubleJumpUsed = false;

                }


                else if (velocity.Y < 0)
                {
                    // ceiling

                    position.Y =
                        tile.Bottom;

                }


                velocity.Y = 0;


                bounds = Bounds;

            }

        
            // Gravity moves the player less than a pixel per frame while standing,
            // which rounds to "not touching" every few frames. Probe 1px below so
            // standing stays standing (this caused the run animation to restart).
            if (!_grounded && velocity.Y >= 0)
            {
                Rectangle probe = new Rectangle(Bounds.X, Bounds.Bottom, Bounds.Width, 1);

                foreach (Rectangle tile in solids)
                {
                    if (probe.Intersects(tile))
                    {
                        // snap the feet exactly onto the tile: otherwise gravity keeps
                        // sinking the position by a fraction of a pixel each frame and
                        // the sprite visibly shakes down/up when it rounds
                        position.Y = tile.Top - size.Y;
                        _grounded = true;
                        _doubleJumpUsed = false;
                        velocity.Y = 0;
                        break;
                    }
                }
            }
        }









        // =====================================================
        // MOVING PLATFORM COLLISION
        // =====================================================

        public void ApplyPlatformCollision(
            List<MovingPlatform> platforms)
        {


            foreach (MovingPlatform platform in platforms)
            {


                if (!Bounds.Intersects(
                    platform.Bounds))
                    continue;





                Rectangle overlap =
                    Rectangle.Intersect(
                        Bounds,
                        platform.Bounds);





                if (overlap.Height <
                   overlap.Width
                   &&
                   velocity.Y >= 0
                   &&
                   position.Y <
                   platform.Bounds.Top)
                {


                    position.Y =
                        platform.Bounds.Top -
                        size.Y;



                    velocity.Y =
                        0;



                    _grounded =
                        true;



                    _doubleJumpUsed =
                        false;



                    _standingOnPlatform =
                        true;

                    platform.NotifyStoodOn();



                    position +=
                        platform.Velocity;



                    break;

                }

            }

        }









        public void SetGrounded(
            bool value)
        {

            _grounded =
                value;



            if (value)
            {

                _doubleJumpUsed =
                    false;

            }

        }
        // =====================================================
        // EVOLUTION SYSTEM
        // =====================================================

        public void Evolve(
            int stage)
        {

            if (stage <= EvolutionStage)
                return;



            EvolutionStage =
                stage;



            // Save progression immediately

            SaveProgression();



            if (stage == 1)
            {

                HasShownShootMessage =
                    false;

            }



            if (stage == 2)
            {

                HasShownDoubleJumpMessage =
                    false;

            }

        }








        // =====================================================
        // FORCE SET EVOLUTION
        // USED WHEN CHANGING LEVEL
        // =====================================================

        public void SetEvolutionStage(
            int stage)
        {

            EvolutionStage =
                stage;



            SaveProgression();

        }










        // =====================================================
        // CHECKPOINT SYSTEM
        // =====================================================


        public void SaveCheckpoint()
        {


            SpawnPoint =
                position;




            SaveProgression();


        }








        // =====================================================
        // SAVE PLAYER PROGRESSION
        // =====================================================

        private void SaveProgression()
        {


            // Keep highest evolution

            if (EvolutionStage >
                _checkpointEvolution)
            {

                _checkpointEvolution =
                    EvolutionStage;

            }






            // Keep highest max health

            if (MaxHealth >
                _checkpointMaxHealth)
            {

                _checkpointMaxHealth =
                    MaxHealth;

            }







            // Keep health

            if (Health >
                _checkpointHealth)
            {

                _checkpointHealth =
                    Health;

            }


        }









        // =====================================================
        // RESPAWN
        // =====================================================

        public void RespawnAtCheckpoint()
        {
            _dashTimer = 0;
            _dashCooldownTimer = 0;



            position =
                SpawnPoint;



            velocity =
                Vector2.Zero;





            EvolutionStage =
                _checkpointEvolution;



            MaxHealth =
                _checkpointMaxHealth;



            Health =
                MaxHealth;





            _grounded =
                false;



            _doubleJumpUsed =
                false;



            _invincibleTimer =
                _invincibleDuration;


        }









        // =====================================================
        // HEALTH SYSTEM
        // =====================================================


        public void IncreaseHealth()
        {

            MaxHealth++;



            Health =
                MaxHealth;



            SaveProgression();

        }






        public void HealFull()
        {

            Health =
                MaxHealth;

        }









        // =====================================================
        // LEVEL TRANSITION SUPPORT
        // =====================================================


        public PlayerData GetPlayerData()
        {

            return new PlayerData
            {

                EvolutionStage =
                    EvolutionStage,


                Health =
                    Health,


                MaxHealth =
                    MaxHealth,


                HasShownShootMessage =
                    HasShownShootMessage,


                HasShownDoubleJumpMessage =
                    HasShownDoubleJumpMessage,


                SpawnPoint =
                    SpawnPoint

            };

        }





        // =====================================================
        // LOAD PLAYER DATA
        // =====================================================

        public void LoadPlayerData(
            PlayerData data)
        {

            EvolutionStage =
                data.EvolutionStage;


            Health =
                data.Health;


            MaxHealth =
                data.MaxHealth;


            HasShownShootMessage =
                data.HasShownShootMessage;


            HasShownDoubleJumpMessage =
                data.HasShownDoubleJumpMessage;


            SpawnPoint =
                data.SpawnPoint;


            UpdateEvolutionSprite();

        }
        // =====================================================
        // DAMAGE SYSTEM
        // =====================================================

        public void TakeDamage(
            int damage)
        {
            if (IsDashing)
                return;


            if (_invincibleTimer > 0)
                return;




            Health -=
                damage;



            if (Health < 0)
            {

                Health =
                    0;

            }





            _invincibleTimer =
                _invincibleDuration;




            // knockback

            velocity.Y =
                -350f;

            AudioManager.Play("hurt");

            Hurt?.Invoke();

        }








        // =====================================================
        // ENEMY COLLISION
        // =====================================================

        public void CheckEnemyCollision(
            List<Enemy> enemies)
        {
            if (IsDashing)
                return;


            if (_invincibleTimer > 0)
                return;





            foreach (Enemy enemy in enemies)
            {

                if (enemy.IsDead || enemy.Despawned || enemy.ContactDamage <= 0)
                    continue;





                if (PixelCollision.Overlaps(GetShape(), enemy.GetShape()))
                {


                    TakeDamage(enemy.ContactDamage);




                    if (position.X <
                       enemy.position.X)
                    {

                        velocity.X = -240f; // knockback (moves with collision, never into walls)

                    }
                    else
                    {

                        velocity.X = 240f;

                    }




                    break;

                }

            }

        }









        // =====================================================
        // DEATH SYSTEM
        // =====================================================

        public bool IsDead()
        {

            return Health <= 0;

        }








        public void CheckDeath()
        {

            if (Health <= 0)
            {
                Died?.Invoke();


                RespawnAtCheckpoint();

            }

        }









        // =====================================================
        // RESET
        // =====================================================

        public void ResetVelocity()
        {

            velocity =
                Vector2.Zero;

        }









        // =====================================================
        // DRAW
        // =====================================================

        public override void Draw(
            SpriteBatch spriteBatch)
        {
            Color tint = EvolutionTint();

            // blink while invincible
            if (_invincibleTimer > 0 &&
                !IsDashing &&
                ((int)(_invincibleTimer * 20f) % 2 == 0))
            {
                tint *= 0.35f;
            }

            SpriteEffects flip =
                FacingDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            // -------------------------------------------------
            // SPRITE SHEET (used automatically once player.png exists)
            // -------------------------------------------------
            if (TextureManager.Player != null &&
                _animation.CurrentFrame != Rectangle.Empty)
            {
                Rectangle frame = _animation.CurrentFrame;

                spriteBatch.Draw(
                    TextureManager.Player,
                    SpriteDestination(frame),
                    frame,
                    tint,
                    0f,
                    Vector2.Zero,
                    flip,
                    0f);

                return;
            }

            // -------------------------------------------------
            // PLACEHOLDER ROBOT (no art yet)
            // -------------------------------------------------
            int w = (int)(size.X * _scaleX);
            int h = (int)(size.Y * _scaleY);

            Rectangle body = new Rectangle(
                (int)(position.X + (size.X - w) / 2f),
                (int)(position.Y + size.Y - h),
                w,
                h);

            // dash after-image
            if (IsDashing)
            {
                Rectangle ghost = body;
                ghost.X -= FacingDirection * 14;

                spriteBatch.Draw(
                    TextureManager.Pixel,
                    ghost,
                    Color.Gold * 0.35f);
            }

            spriteBatch.Draw(
                TextureManager.Pixel,
                body,
                IsAttacking ? Color.Orange * (tint.A / 255f) : tint);

            // antenna
            spriteBatch.Draw(
                TextureManager.Pixel,
                new Rectangle(body.Center.X - 1, body.Y - 6, 2, 6),
                Color.Gray * (tint.A / 255f));

            // eye shows facing direction
            int eyeX =
                FacingDirection == 1
                ? body.Right - 12
                : body.X + 4;

            spriteBatch.Draw(
                TextureManager.Pixel,
                new Rectangle(eyeX, body.Y + 8, 8, 6),
                _invincibleTimer > 0 ? Color.Red : Color.Cyan);

            // melee slash
            if (IsAttacking)
            {
                spriteBatch.Draw(
                    TextureManager.Pixel,
                    AttackHitbox,
                    Color.White * 0.25f);
            }
        }

        private Color EvolutionTint()
        {
            switch (EvolutionStage)
            {
                case 1:
                    return Color.LightSkyBlue;

                case 2:
                    return Color.LightGreen;

                case 3:
                    return Color.Cyan;

                case 4:
                    return Color.Gold;

                default:
                    return Color.White;
            }
        }

        // =====================================================
        // DASH
        // =====================================================

        private void HandleDash(
            InputManager input,
            float dt)
        {
            if (_dashCooldownTimer > 0)
                _dashCooldownTimer -= dt;

            if (_dashTimer > 0)
            {
                _dashTimer -= dt;
                velocity.X = FacingDirection * DashSpeed;
                return;
            }

            if (EvolutionStage >= 4 &&
                _dashCooldownTimer <= 0 &&
                input.DashPressed())
            {
                _dashTimer = DashDuration;
                _dashCooldownTimer = DashCooldown;

                velocity.X = FacingDirection * DashSpeed;
                velocity.Y = 0;

                AudioManager.Play("dash");
            }
        }

        // =====================================================
        // ANIMATION
        // Sprite sheet layout expected in Content/Textures/player.png:
        // 32x32 frames, one animation per row:
        // 0 idle(4)  1 run(6)  2 jump(2)  3 fall(2)  4 attack(4)  5 dash(2)
        // =====================================================

        private void SetupAnimations()
        {
            AddRow("idle", 0, 4, 0.22f, true);
            AddRow("run", 1, 6, 0.11f, true);
            AddRow("jump", 2, 2, 0.12f, false);
            AddRow("fall", 3, 2, 0.14f, true);
            AddRow("attack", 4, 4, 0.075f, false);
            AddRow("dash", 5, 2, 0.07f, true);

            _animation.Play("idle");
        }

        private void AddRow(
            string name,
            int row,
            int frameCount,
            float frameTime,
            bool loop)
        {
            const int frameSize = 32;

            List<Rectangle> frames = new();

            for (int i = 0; i < frameCount; i++)
            {
                frames.Add(new Rectangle(
                    i * frameSize,
                    row * frameSize,
                    frameSize,
                    frameSize));
            }

            _animation.AddAnimation(
                name,
                frames,
                frameTime,
                loop);
        }

        private void UpdateAnimationState(
            float dt)
        {
            string state;

            if (IsDashing)
                state = "dash";
            else if (IsAttacking)
                state = "attack";
            else if (!_grounded && (_airTime > 0.08f || velocity.Y < -60f))
                state = velocity.Y < 0 ? "jump" : "fall";
            else if (Math.Abs(velocity.X) > 25f)
                state = "run";
            else
                state = "idle";

            _airTime = _grounded ? 0f : _airTime + dt;

            _animation.Play(state);

            // squash when landing, stretch while in the air
            if (_grounded && !_wasGrounded)
            {
                _landSquash = 0.12f;
            }

            _wasGrounded = _grounded;

            if (_landSquash > 0)
            {
                _landSquash -= dt;
                _scaleX = 1.2f;
                _scaleY = 0.8f;
            }
            else if (!_grounded)
            {
                float stretch =
                    MathHelper.Clamp(
                        System.Math.Abs(velocity.Y) / 900f,
                        0f,
                        1f) * 0.15f;

                _scaleY = 1f + stretch;
                _scaleX = 1f - stretch * 0.6f;
            }
            else
            {
                _scaleX = MathHelper.Lerp(_scaleX, 1f, 0.3f);
                _scaleY = MathHelper.Lerp(_scaleY, 1f, 0.3f);
            }
        }

        // =====================================================
        // SAVE / LOAD
        // =====================================================

        // =====================================================
        // PIXEL SHAPE (matches what Draw puts on screen)
        // =====================================================

        public override SpriteShape GetShape()
        {
            if (TextureManager.Player == null || _animation.CurrentFrame == Rectangle.Empty)
                return SpriteShape.FromRect(Bounds);

            Rectangle frame = _animation.CurrentFrame;
            return SpriteShape.FromTexture(TextureManager.Player, frame, SpriteDestination(frame), FacingDirection == -1);
        }

        // =====================================================
        // SPRITE BODY
        // The robot only fills part of its 32x32 frame. The physics box is
        // sized to the visible pixels so R-01 touches walls and ledges with
        // its actual body, not with transparent padding.
        // =====================================================

        private void ApplySpriteBody()
        {
            Texture2D texture = TextureManager.Player;

            if (texture == null)
                return;

            if (_spriteBody == null)
            {
                bool[] mask = PixelCollision.Mask(texture);
                int left = 32, right = 0, top = 32, bottom = 0;

                // union of the idle and run frames (rows 0 and 1)
                for (int y = 0; y < Math.Min(64, texture.Height); y++)
                {
                    for (int x = 0; x < texture.Width; x++)
                    {
                        if (!mask[y * texture.Width + x])
                            continue;

                        int fx = x % 32;
                        int fy = y % 32;

                        left = Math.Min(left, fx);
                        right = Math.Max(right, fx + 1);
                        top = Math.Min(top, fy);
                        bottom = Math.Max(bottom, fy + 1);
                    }
                }

                if (right <= left || bottom <= top)
                {
                    left = 0; right = 32; top = 0; bottom = 32;
                }

                _spriteBody = new Rectangle(left, top, right - left, bottom - top);
            }

            Rectangle body = _spriteBody.Value;

            // slightly narrower than the art so it never snags on corners
            size = new Vector2(Math.Max(10, body.Width - 2), Math.Max(16, body.Height - 1));
        }

        // where the 32x32 frame goes so the body sits exactly on the hitbox
        private Rectangle SpriteDestination(Rectangle frame)
        {
            Rectangle body = _spriteBody ?? new Rectangle(0, 0, frame.Width, frame.Height);

            float bodyCentre = body.X + body.Width / 2f;

            if (FacingDirection == -1)
                bodyCentre = frame.Width - bodyCentre;

            int x = (int)Math.Round(position.X + size.X / 2f - bodyCentre);
            int y = (int)Math.Round(position.Y + size.Y - body.Bottom);

            return new Rectangle(x, y, frame.Width, frame.Height);
        }

        // used when an area restarts: progress goes back to what it was on entry
        public void ResetProgress(int evolutionStage, int maxHealth)
        {
            EvolutionStage = evolutionStage;
            MaxHealth = maxHealth;
            Health = maxHealth;

            _checkpointEvolution = evolutionStage;
            _checkpointMaxHealth = maxHealth;
            _checkpointHealth = maxHealth;
        }

        public void RestoreFromSave(
            int evolutionStage,
            int maxHealth,
            Vector2 spawnPoint)
        {
            EvolutionStage = evolutionStage;
            MaxHealth = maxHealth;
            Health = maxHealth;
            SpawnPoint = spawnPoint;
            position = spawnPoint;
            velocity = Vector2.Zero;

            // make sure death/respawn keeps the loaded progress
            SaveProgression();
        }
    }
}
