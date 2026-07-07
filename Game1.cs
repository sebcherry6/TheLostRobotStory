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



        private readonly List<Projectile> _projectiles =
            new();



        private readonly ParticleManager _particles =
            new();



        private Camera _camera;



        private SpriteFont _uiFont;



        private Door _activeDoor;




        // =====================================================
        // LEVEL TRANSITION
        // =====================================================

        private bool _isTransitioning;

        private bool _levelLoaded;

        private float _fade;




        // =====================================================
        // INPUT
        // =====================================================

        private KeyboardState _keyboard;

        private KeyboardState _previousKeyboard;




        // =====================================================
        // CAMERA SHAKE
        // =====================================================

        private float _shakeTimer;

        private float _shakeMagnitude;

        private Vector2 _shakeOffset;




        // =====================================================
        // ATTACK
        // =====================================================

        private float _attackTimer;

        private const float AttackCooldown = 0.25f;




        // =====================================================
        // POPUP MESSAGES
        // =====================================================

        private string _messageText = "";

        private float _messageTimer;


        private bool _shownControls;




        public Game1()
        {

            _graphics =
                new GraphicsDeviceManager(this);


            Content.RootDirectory =
                "Content";


            IsMouseVisible = true;

        }




        protected override void Initialize()
        {

            _camera =
                new Camera();



            _levelManager =
                new LevelManager();








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

            _hud =
    new HUD(_uiFont);



            _levelManager.LoadLevel(
                "level1.txt");



            _level =
                _levelManager.CurrentLevel;



            _player =
                new Player(
                    _level._spawnPoint);



            ShowMessage(
                "A - Left\nD - Right\nSPACE - Jump\nJ - Attack",
                5f);

        }







        protected override void Update(
            GameTime gameTime)
        {

            if (Keyboard.GetState()
                .IsKeyDown(Keys.Escape))
            {
                Exit();
            }



            float dt =
                (float)
                gameTime.ElapsedGameTime.TotalSeconds;




            // =====================================================
            // INPUT
            // =====================================================

            _previousKeyboard =
                _keyboard;


            _keyboard =
                Keyboard.GetState();





            // =====================================================
            // MESSAGE TIMER
            // =====================================================

            if (_messageTimer > 0)
            {

                _messageTimer -= dt;


                if (_messageTimer <= 0)
                {
                    _messageText = "";
                }

            }





            // =====================================================
            // ATTACK TIMER
            // =====================================================

            if (_attackTimer > 0)
            {
                _attackTimer -= dt;
            }







            // =====================================================
            // LEVEL TRANSITION
            // =====================================================

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



                    _player.SaveCheckpoint();



                    _projectiles.Clear();



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







            // =====================================================
            // PLAYER UPDATE
            // =====================================================


            _player.Update(
                gameTime,
                _keyboard,
                _previousKeyboard,
                _projectiles);



            _player.ApplyCollision(
                _level._solids);







            // =====================================================
            // ENERGY CRYSTALS
            // =====================================================


            foreach (var crystal in _level._collectibles)
            {

                if (!crystal.IsCollected &&
                   _player.Bounds.Intersects(
                   crystal.Bounds))
                {

                    crystal.Collect(_player);



                    _particles.SpawnExplosion(
                        crystal.Position,
                        Color.Yellow);

                }

            }







            // =====================================================
            // HEALTH UPGRADES
            // =====================================================

            foreach (var upgrade in _level._healthUpgrades)
            {

                if (!upgrade.IsCollected &&
                   _player.Bounds.Intersects(
                   upgrade.Bounds))
                {

                    upgrade.Collect(_player);


                    _player.IncreaseHealth();


                    ShowMessage(
                        "+ Health increased!",
                        3f);


                    _particles.SpawnExplosion(
                        upgrade.Position,
                        Color.Red);

                }

            }







            // =====================================================
            // CHECKPOINTS
            // =====================================================


            foreach (var checkpoint in _level._checkpoints)
            {

                if (_player.Bounds.Intersects(
                    checkpoint.Bounds))
                {

                    checkpoint.Activate(
                        _player);

                }

            }
            // =====================================================
            // EVOLUTION CORES
            // =====================================================

            foreach (var core in _level._evolutionCores)
            {

                core.Update(gameTime);



                if (!core.Collected &&
                   _player.Bounds.Intersects(
                   core.Bounds))
                {

                    int oldStage =
                        _player.EvolutionStage;



                    core.Collect(
                        _player);



                    _particles.SpawnExplosion(
                        core.Position,
                        Color.Cyan);



                    StartShake(
                        0.15f,
                        3f);



                    if (_player.EvolutionStage > oldStage)
                    {

                        if (_player.EvolutionStage == 1)
                        {

                            ShowMessage(
                                "Evolution 1 unlocked!\nPress K to shoot",
                                4f);

                        }



                        if (_player.EvolutionStage == 2)
                        {

                            ShowMessage(
                                "Double jump and super shot unlocked!",
                                5f);

                        }



                        if (_player.EvolutionStage == 3)
                        {

                            ShowMessage(
                                "Final evolution unlocked!",
                                5f);

                        }

                    }

                }

            }







            // =====================================================
            // PROJECTILES
            // =====================================================

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








            // =====================================================
            // DOORS
            // =====================================================


            bool cleared =
                _level.IsCleared();




            foreach (var door in _level._doors)
            {

                door.CanOpen =
                    cleared;

            }





            _activeDoor = null;




            foreach (var door in _level._doors)
            {

                if (_player.Bounds.Intersects(
                    door.Bounds))
                {

                    _activeDoor =
                        door;




                    if (door.CanOpen &&
                       _keyboard.IsKeyDown(Keys.E) &&
                       !_previousKeyboard.IsKeyDown(Keys.E))
                    {

                        _isTransitioning =
                            true;


                        _fade =
                            0;


                        break;

                    }

                }

            }









            // =====================================================
            // ENEMIES + BOSS
            // =====================================================


            for (int i = _level._enemies.Count - 1;
                i >= 0;
                i--)
            {

                Enemy enemy =
                    _level._enemies[i];




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
                            Color.Orange);



                        StartShake(
                            0.1f,
                            2f);

                    }

                }





                if (enemy.IsDead)
                {

                    _particles.SpawnExplosion(
                        enemy.position,
                        Color.Red);



                    StartShake(
                        0.2f,
                        4f);



                    _level._enemies.RemoveAt(i);

                }

            }








            // =====================================================
            // PLAYER DAMAGE
            // =====================================================


            _player.CheckEnemyCollision(
                _level._enemies);



            _player.CheckDeath();







            // =====================================================
            // CAMERA
            // =====================================================

            _camera.Follow(
                _player.position);







            // =====================================================
            // PARTICLES
            // =====================================================

            _particles.Update(
                gameTime);




            UpdateShake(
                dt);



            base.Update(gameTime);

        }
        protected override void Draw(
    GameTime gameTime)
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





            // =====================================================
            // WORLD DRAW
            // =====================================================

            _spriteBatch.Begin(
                transformMatrix: cameraMatrix);



            _level.Draw(
                _spriteBatch);



            _player.Draw(
                _spriteBatch);




            foreach (var projectile in _projectiles)
            {

                projectile.Draw(
                    _spriteBatch);

            }




            _particles.Draw(
                _spriteBatch,
                TextureManager.Pixel);







            // =====================================================
            // BOSS HEALTH BAR
            // =====================================================

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



                    Rectangle back =
                        new Rectangle(
                            boss.Bounds.Center.X - 50,
                            boss.Bounds.Top - 20,
                            100,
                            8);



                    Rectangle front =
                        new Rectangle(
                            boss.Bounds.Center.X - 50,
                            boss.Bounds.Top - 20,
                            (int)(100 * hp),
                            8);



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









            // =====================================================
            // UI DRAW
            // =====================================================


            _spriteBatch.Begin();





            // HEALTH + COUNTERS

            _hud.Draw(
                _spriteBatch,
                _player,
                _level.CollectiblesRemaining,
                _level.EnemiesRemaining);








            // =====================================================
            // DOOR MESSAGE
            // =====================================================

            if (_activeDoor != null &&
               !_isTransitioning)
            {

                string text =
                    _activeDoor.CanOpen
                    ?
                    "Press E to enter"
                    :
                    $"Collect crystals and defeat enemies\n" +
                    $"Crystals: {_level.CollectiblesRemaining}  " +
                    $"Enemies: {_level.EnemiesRemaining}";




                Vector2 size =
                    _uiFont.MeasureString(text);



                _spriteBatch.DrawString(
                    _uiFont,
                    text,
                    new Vector2(
                        (_graphics.PreferredBackBufferWidth / 2)
                        - size.X / 2,
                        180),
                    Color.White);

            }







            // =====================================================
            // POPUP MESSAGE
            // =====================================================

            if (!string.IsNullOrEmpty(_messageText))
            {

                Vector2 size =
                    _uiFont.MeasureString(
                        _messageText);



                _spriteBatch.DrawString(
                    _uiFont,
                    _messageText,
                    new Vector2(
                        (_graphics.PreferredBackBufferWidth / 2)
                        - size.X / 2,
                        80),
                    Color.White);

            }






            _spriteBatch.End();








            // =====================================================
            // FADE EFFECT
            // =====================================================


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







        // =====================================================
        // POPUP MESSAGE SYSTEM
        // =====================================================



        private void ShowMessage(
            string message,
            float duration)
        {

            _messageText =
                message;


            _messageTimer =
                duration;

        }









        // =====================================================
        // CAMERA SHAKE
        // =====================================================


        private void StartShake(
            float duration,
            float magnitude)
        {

            _shakeTimer =
                duration;


            _shakeMagnitude =
                magnitude;

        }





        private void UpdateShake(
            float dt)
        {

            if (_shakeTimer > 0)
            {

                _shakeTimer -= dt;



                _shakeOffset =
                    new Vector2(

                    (float)(Random.Shared.NextDouble() * 2 - 1)
                    *
                    _shakeMagnitude,


                    (float)(Random.Shared.NextDouble() * 2 - 1)
                    *
                    _shakeMagnitude);

            }

            else
            {

                _shakeOffset =
                    Vector2.Zero;

            }

        }


    }
}

