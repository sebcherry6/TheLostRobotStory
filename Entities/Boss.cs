using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TheLostRobotStory.Core;

namespace TheLostRobotStory.Entities
{
    public class Boss : Enemy
    {

        public int MaxHealth = 30;


        public bool IsDead => Health <= 0;


        // =========================
        // FLYING MOVEMENT
        // =========================

        private Vector2 _flyVelocity;

        private float _flySpeed = 140f;

        private float _hoverHeight = 100f;

        private float _hoverTimer;



        // =========================
        // ATTACKS
        // =========================

        private float _shootTimer = 1.5f;

        private float _dashTimer = 5f;

        private float _slamTimer = 6f;


        private bool _dashing;

        private float _dashDuration;

        private Vector2 _dashDirection;



        // =========================
        // DAMAGE
        // =========================

        private float _bossHitCooldown;

        private bool _damagedFlash;



        // =========================
        // GRAVITY PULL
        // =========================

        private float _pullRadius = 350f;

        private float _pullForce = 120f;



        // =========================
        // ARENA
        // =========================

        public static float ArenaShrink = 0;

        public static Action BossSlamEvent;



        public Boss(Vector2 startPos)
            : base(startPos, EnemyType.Laser)
        {

            position = startPos;

            size = new Vector2(96, 96);


            // IMPORTANT
            Health = MaxHealth;

        }





        // =========================
        // PHASES
        // =========================

        private int Phase
        {
            get
            {

                float hp =
                    (float)Health / MaxHealth;


                if (hp > .66f)
                    return 1;


                if (hp > .33f)
                    return 2;


                return 3;

            }
        }




        // =========================
        // DAMAGE
        // =========================

        public override void TakeDamage(int damage)
        {

            if (_bossHitCooldown > 0)
                return;



            Health -= damage;


            _bossHitCooldown = .25f;

            _damagedFlash = true;

        }





        // =========================
        // UPDATE
        // =========================

        public void Update(
            List<Rectangle> solids,
            Player player,
            List<Projectile> projectiles,
            float dt)
        {

            if (IsDead)
                return;



            if (_bossHitCooldown > 0)
                _bossHitCooldown -= dt;



            _shootTimer -= dt;

            _dashTimer -= dt;

            _slamTimer -= dt;



            FlyTowardsPlayer(player, dt);



            PullPlayer(player, dt);



            if (_dashTimer <= 0 && !_dashing)
            {
                StartDash(player);
            }



            if (_dashing)
            {

                position +=
                    _dashDirection *
                    500f *
                    dt;



                _dashDuration -= dt;



                if (_dashDuration <= 0)
                    _dashing = false;

            }



            if (_shootTimer <= 0)
            {

                _shootTimer =
                    ShootRate();


                Shoot(player, projectiles);

            }



            if (_slamTimer <= 0)
            {

                _slamTimer = 7f;


                BossSlamEvent?.Invoke();


                ArenaShrink += 15;


                if (ArenaShrink > 250)
                    ArenaShrink = 250;

            }

        }





        // =========================
        // FLYING AI
        // =========================

        private void FlyTowardsPlayer(
            Player player,
            float dt)
        {

            _hoverTimer += dt;


            Vector2 target =
                player.position;


            target.Y -= _hoverHeight;



            Vector2 direction =
                target - position;



            if (direction != Vector2.Zero)
                direction.Normalize();



            Vector2 desired =
                direction * _flySpeed;



            _flyVelocity =
                Vector2.Lerp(
                    _flyVelocity,
                    desired,
                    2f * dt);



            position +=
                _flyVelocity * dt;



            position.Y +=
                (float)Math.Sin(_hoverTimer * 3f)
                * 0.5f;

        }




        // =========================
        // DASH ATTACK
        // =========================

        private void StartDash(Player player)
        {

            _dashTimer =
                Phase == 3 ? 3f : 6f;



            _dashDuration = .6f;


            _dashDirection =
                player.position - position;



            if (_dashDirection != Vector2.Zero)
                _dashDirection.Normalize();



            _dashing = true;

        }





        // =========================
        // SHOOT
        // =========================

        private void Shoot(
            Player player,
            List<Projectile> projectiles)
        {


            Vector2 future =
                player.position +
                player.velocity * 20;



            Vector2 direction =
                future - position;



            if (direction != Vector2.Zero)
                direction.Normalize();



            projectiles.Add(
                new Projectile(
                    position +
                    new Vector2(48, 48),
                    direction,
                    false));



            if (Phase >= 2)
            {

                projectiles.Add(
                    new Projectile(
                        position,
                        new Vector2(
                            direction.X,
                            direction.Y - .25f),
                        false));



                projectiles.Add(
                    new Projectile(
                        position,
                        new Vector2(
                            direction.X,
                            direction.Y + .25f),
                        false));

            }


        }



        private float ShootRate()
        {

            if (Phase == 1)
                return 1.5f;


            if (Phase == 2)
                return .9f;


            return .45f;

        }





        // =========================
        // PULL EFFECT
        // =========================

        private void PullPlayer(
            Player player,
            float dt)
        {

            Vector2 pull =
                position - player.position;



            float distance =
                pull.Length();



            if (distance < _pullRadius)
            {

                pull.Normalize();



                player.position +=
                    pull *
                    (_pullForce /
                    Math.Max(distance, 1))
                    * dt;

            }

        }





        // =========================
        // DRAW
        // =========================

        public override void Draw(
            SpriteBatch spriteBatch)
        {

            if (IsDead)
                return;



            Color color;


            if (_damagedFlash)
            {
                color = Color.White;
                _damagedFlash = false;
            }
            else
            {
                color =
                    Phase == 1 ?
                    Color.Black :
                    Phase == 2 ?
                    Color.DarkRed :
                    Color.Purple;
            }



            spriteBatch.Draw(
                TextureManager.Pixel,
                Bounds,
                color);

        }

    }
}