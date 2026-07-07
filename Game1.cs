using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;
using TheLostRobotStory.World;

namespace TheLostRobotStory
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;


        private LevelManager _levelManager;
        private Level _level;


        private Player _player;
        private HUD _hud;


        private List<Projectile> _projectiles = new();



        private SpriteFont _uiFont;


        private ParticleManager _particles =
            new ParticleManager();



        private Door _activeDoor;



        // =========================
        // LEVEL TRANSITION
        // =========================

        private bool _isTransitioning;

        private bool _levelLoaded;

        private float _fade;



        // =========================
        // INPUT
        // =========================

        private KeyboardState _keyboard;

        private KeyboardState _previousKeyboard;



        // =========================
        // CAMERA SHAKE
        // =========================

        private float _shakeTimer;

        private float _shakeMagnitude;

        private Vector2 _shakeOffset;



        private Camera _camera;



        // =========================
        // ATTACK COOLDOWN
        // =========================

        private float _attackTimer;

        private const float AttackCooldown = 0.25f;



        public Game1()
        {
            _graphics =
                new GraphicsDeviceManager(this);


            Content.RootDirectory = "Content";


            IsMouseVisible = true;
        }




        protected override void Initialize()
        {

            _camera =
                new Camera();



            _levelManager =
                new LevelManager();



            _hud =
                new HUD();



            Boss.BossSlamEvent += () =>
            {
                StartShake(
                    0.25f,
                    6f);
            };



            base.Initialize();
        }





        protected override void LoadContent()
        {

            _spriteBatch =
                new SpriteBatch(GraphicsDevice);



            TextureManager.Pixel =
                new Texture2D(
                    GraphicsDevice,
                    1,
                    1);



            TextureManager.Pixel.SetData(
                new[]
                {
                    Color.White
                });



            _uiFont =
                Content.Load<SpriteFont>(
                    "UIFont");



            _levelManager.LoadLevel(
                "level1.txt");



            _level =
                _levelManager.CurrentLevel;



            _player =
                new Player(
                    _level._spawnPoint);

        }






        protected override void Update(GameTime gameTime)
        {

            if (Keyboard.GetState()
                .IsKeyDown(Keys.Escape))
            {
                Exit();
            }



            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;



            // INPUT UPDATE

            _previousKeyboard =
                _keyboard;


            _keyboard =
                Keyboard.GetState();




            // ATTACK TIMER

            if (_attackTimer > 0)
                _attackTimer -= dt;





            // =========================
            // TRANSITION
            // =========================

            if (_isTransitioning)
            {

                _fade += dt;



                if (_fade >= 1f &&
                    !_levelLoaded)
                {

                    _levelManager.LoadLevel(
                        _activeDoor.DestinationLevel);



                    _level =
                        _levelManager.CurrentLevel;



                    _player.position =
                        _level._spawnPoint;



                    _levelLoaded = true;

                }



                if (_fade >= 2f)
                {

                    _fade = 0;

                    _levelLoaded = false;

                    _isTransitioning = false;

                }



                return;
            }





            // =========================
            // PLAYER UPDATE
            // =========================

            _player.Update(
                gameTime,
                _keyboard,
                _previousKeyboard,
                _projectiles);



            _player.ApplyCollision(
                _level._solids);






            // =========================
            // NORMAL COLLECTIBLES
            // =========================

            foreach (var collectible in _level._collectibles)
            {

                if (!collectible.IsCollected &&
                    _player.Bounds.Intersects(
                    collectible.Bounds))
                {

                    collectible.IsCollected = true;

                }

            }





            // =========================
            // EVOLUTION CORES
            // =========================

            foreach (var core in _level._evolutionCores)
            {

                core.Update(gameTime);



                if (!core.Collected &&
                    _player.Bounds.Intersects(
                    core.Bounds))
                {

                    core.Collect(
                        _player);



                    _particles.SpawnExplosion(
                        core.Position,
                        Color.Cyan);



                    StartShake(
                        0.15f,
                        3f);

                }

            }






            // =========================
            // PROJECTILES
            // =========================

            for (int i = _projectiles.Count - 1;
                i >= 0;
                i--)
            {

                Projectile projectile =
                    _projectiles[i];



                projectile.Update(
                    _level._solids);



                foreach (var enemy in _level._enemies)
                {

                    projectile.HitEnemy(
                        enemy);

                }



                projectile.HitPlayer(
                    _player);



                if (projectile.IsDead)
                {
                    _projectiles.RemoveAt(i);
                }

            }






            // =========================
            // DOORS
            // =========================

            bool cleared =
                _level.IsCleared();



            foreach (var door in _level._doors)
            {
                door.CanOpen = cleared;
            }



            _activeDoor = null;



            foreach (var door in _level._doors)
            {

                if (_player.Bounds.Intersects(
                    door.Bounds))
                {

                    _activeDoor = door;



                    if (door.CanOpen &&
                        _keyboard.IsKeyDown(Keys.E) &&
                        !_previousKeyboard.IsKeyDown(Keys.E))
                    {

                        _isTransitioning = true;

                        _fade = 0;

                        break;

                    }

                }

            }
            // =========================
            // CAMERA SHAKE
            // =========================

            if (_shakeTimer > 0)
            {
                _shakeTimer -= dt;


                _shakeOffset = new Vector2(
                    (float)(Random.Shared.NextDouble() * 2 - 1)
                    * _shakeMagnitude,

                    (float)(Random.Shared.NextDouble() * 2 - 1)
                    * _shakeMagnitude
                );
            }
            else
            {
                _shakeOffset = Vector2.Zero;
            }





            // =========================
            // ENEMIES + BOSS
            // =========================

            for (int i = _level._enemies.Count - 1;
                i >= 0;
                i--)
            {

                Enemy enemy =
                    _level._enemies[i];



                // BOSS UPDATE

                if (enemy is Boss boss)
                {

                    boss.Update(
                        _level._solids,
                        _player,
                        _projectiles,
                        dt);

                }
                else
                {

                    enemy.Update(
                        _level._solids,
                        _player,
                        _projectiles);

                }




                // PLAYER ATTACK DAMAGE

                if (_player.AttackHitbox.Intersects(
                    enemy.Bounds))
                {

                    if (_attackTimer <= 0)
                    {

                        enemy.TakeDamage(1);


                        _attackTimer =
                            AttackCooldown;



                        _particles.SpawnExplosion(
                            enemy.position,
                            Color.Yellow);



                        StartShake(
                            0.1f,
                            2f);

                    }

                }





                // REMOVE DEAD ENEMY

                if (enemy.IsDead)
                {

                    _particles.SpawnExplosion(
                        enemy.position,
                        Color.OrangeRed);



                    StartShake(
                        0.2f,
                        4f);



                    _level._enemies.RemoveAt(i);

                }

            }






            // =========================
            // PLAYER DAMAGE
            // =========================

            _player.CheckEnemyCollision(
                _level._enemies);



            _player.CheckDeath();






            // =========================
            // CAMERA
            // =========================

            _camera.Follow(
                _player.position);




            // =========================
            // PARTICLES
            // =========================

            _particles.Update(
                gameTime);



            base.Update(gameTime);

        }







        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.Clear(
                Color.CornflowerBlue);



            Matrix cameraMatrix =
                _camera.GetViewMatrix()
                *
                Matrix.CreateTranslation(
                    new Vector3(
                        _shakeOffset,
                        0));






            // =========================
            // WORLD DRAW
            // =========================

            _spriteBatch.Begin(
                transformMatrix: cameraMatrix);



            _level.Draw(
                _spriteBatch);



            _player.Draw(
                _spriteBatch);



            _particles.Draw(
                _spriteBatch,
                TextureManager.Pixel);





            foreach (var projectile in _projectiles)
            {
                projectile.Draw(
                    _spriteBatch);
            }






            // =========================
            // BOSS HP BAR
            // WORLD SPACE
            // =========================

            foreach (var enemy in _level._enemies)
            {

                if (enemy is Boss boss)
                {

                    float hp =
                        MathHelper.Clamp(
                        (float)boss.Health /
                        boss.MaxHealth,
                        0,
                        1);



                    Vector2 hpPos =
                        new Vector2(
                            boss.Bounds.Center.X - 40,
                            boss.Bounds.Top - 15);



                    Rectangle back =
                        new Rectangle(
                            (int)hpPos.X,
                            (int)hpPos.Y,
                            80,
                            7);



                    Rectangle front =
                        new Rectangle(
                            (int)hpPos.X,
                            (int)hpPos.Y,
                            (int)(80 * hp),
                            7);



                    _spriteBatch.Draw(
                        TextureManager.Pixel,
                        back,
                        Color.DarkRed);



                    _spriteBatch.Draw(
                        TextureManager.Pixel,
                        front,
                        Color.LimeGreen);

                }

            }




            _spriteBatch.End();






            // =========================
            // UI DRAW
            // =========================

            _spriteBatch.Begin();



            _hud.Draw(
                _spriteBatch,
                _player.Health);






            // DOOR MESSAGE

            if (_activeDoor != null &&
                !_isTransitioning)
            {

                string text =
                    _activeDoor.CanOpen
                    ? "Press E to enter"
                    : "Find all collectibles";



                Vector2 size =
                    _uiFont.MeasureString(text);



                Vector2 pos =
                    new Vector2(
                        (_graphics.PreferredBackBufferWidth / 2)
                        - size.X / 2,
                        200);



                _spriteBatch.DrawString(
                    _uiFont,
                    text,
                    pos,
                    Color.White);

            }




            _spriteBatch.End();








            // =========================
            // FADE SCREEN
            // =========================

            _spriteBatch.Begin();



            if (_isTransitioning)
            {

                _spriteBatch.Draw(
                    TextureManager.Pixel,
                    new Rectangle(
                        0,
                        0,
                        _graphics.PreferredBackBufferWidth,
                        _graphics.PreferredBackBufferHeight),
                    Color.Black *
                    MathHelper.Clamp(
                        _fade,
                        0,
                        1));

            }



            _spriteBatch.End();




            base.Draw(gameTime);

        }






        private void StartShake(
            float duration,
            float magnitude)
        {

            _shakeTimer =
                duration;


            _shakeMagnitude =
                magnitude;

        }

    }
}